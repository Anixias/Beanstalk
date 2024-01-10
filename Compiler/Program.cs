using System.Diagnostics;
using System.Text;
using Beanstalk.Analysis.Diagnostics;
using Beanstalk.Analysis.Semantics;
using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;
using Beanstalk.CodeGen;

namespace Compiler;

internal struct ProgramArgs
{
	public string? InputPath { get; private set; }
	public string? OutputPath { get; private set; }
	public int? OptimizationLevel { get; private set; }
	public Target? Target { get; private set; }

	public static ProgramArgs? Parse(string[] args)
	{
		var programArgs = new ProgramArgs();
		
		for (var i = 0; i < args.Length; i++)
		{
			var command = args[i];
			try
			{
				i = ParseCommand(command, i + 1) - 1;
			}
			catch
			{
				return null;
			}
		}

		return programArgs;

		int ParseCommand(string command, int position)
		{
			switch (command)
			{
				case "-in":
					programArgs.InputPath = Read();
					break;
				case "-out":
					programArgs.OutputPath = Read();
					break;
				case "-opt":
					programArgs.OptimizationLevel = int.Parse(Read());
					break;
				case "-target":
					programArgs.Target = new Target(Read());
					break;
				default:
					throw new ArgumentException($"Invalid parameter '{command}'.", nameof(command));
			}

			return position;

			string Read() => args[position++];
		}
	}
}

internal static class Program
{
	private static readonly object ConsoleLock = new();
	
	private static async Task Main(string[] args)
	{
		if (args.Length == 0)
			RunRepl();
		else
			await Compile(args);
	}

	private static void Print(object? obj)
	{
		Console.Write(obj?.ToString() ?? "null");
	}

	private static void PrintLine()
	{
		Console.WriteLine();
	}

	private static void PrintLine(object? obj)
	{
		Console.WriteLine(obj?.ToString() ?? "null");
	}

	private static void PrintLineNumber(int line)
	{
		Console.ForegroundColor = ConsoleColor.DarkGray;
		Print($"{line:0000} ");
		Console.ResetColor();
	}

	private static void RunRepl()
	{
		while (true)
		{
			var sourceStringBuilder = new StringBuilder();
			var line = 1;
			PrintLineNumber(line);
			var inputStringBuilder = new StringBuilder();
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Enter)
				{
					sourceStringBuilder.AppendLine(inputStringBuilder.ToString());
					inputStringBuilder.Clear();
					PrintLine();
					
					if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
						break;
					
					PrintLineNumber(++line);
					continue;
				}

				if (key.Key == ConsoleKey.Backspace)
				{
					if (inputStringBuilder.Length <= 0)
						continue;
					
					Print("\b \b");
					inputStringBuilder.Remove(inputStringBuilder.Length - 1, 1);
					continue;
				}
				
				inputStringBuilder.Append(key.KeyChar);
				Print(key.KeyChar);
			}

			var source = sourceStringBuilder.ToString();
			if (string.IsNullOrWhiteSpace(source))
				break;

			Interpret(source);
			PrintLine();
		}
	}

	private readonly struct FileDiagnostics
	{
		public readonly string workingDirectory;
		public readonly string filePath;
		public readonly DiagnosticList parseExceptions = [];
		public readonly List<CollectionException> collectionExceptions = [];
		public readonly List<ResolutionException> resolutionExceptions = [];

		public FileDiagnostics(string workingDirectory, string filePath)
		{
			this.workingDirectory = workingDirectory;
			this.filePath = filePath;
		}

		public void Print()
		{
			if (parseExceptions.ErrorCount > 0)
				PrintDiagnosticList("Parsing Error(s)", parseExceptions.Errors, DiagnosticSeverity.Error);
			
			if (parseExceptions.WarningCount > 0)
				PrintDiagnosticList("Parsing Warning(s)", parseExceptions.Warnings, DiagnosticSeverity.Warning);
			
			if (collectionExceptions.Count > 0)
				PrintErrorList("Collection Error(s)", collectionExceptions);
			
			if (resolutionExceptions.Count > 0)
				PrintErrorList("Resolution Error(s)", resolutionExceptions);
		}

		private void PrintErrorList(string errorLabel, IEnumerable<Exception> exceptions)
		{
			var relativePath = Path.GetRelativePath(workingDirectory, filePath);
			var output = $"------------ {relativePath} ------------\n"
			             + $"{errorLabel}:\n"
			             + $"{string.Join('\n', exceptions.Select(e => $"\t{e.Message}"))}\n";
			
			lock (ConsoleLock)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(output);
				Console.ResetColor();
			}
		}

		private void PrintDiagnosticList(string label, IEnumerable<Diagnostic> diagnostics, DiagnosticSeverity severity)
		{
			var relativePath = Path.GetRelativePath(workingDirectory, filePath);
			var color = severity switch
			{
				DiagnosticSeverity.Error => ConsoleColor.Red,
				DiagnosticSeverity.Warning => ConsoleColor.Yellow,
				_ => ConsoleColor.White
			};
			
			var darkColor = severity switch
			{
				DiagnosticSeverity.Error => ConsoleColor.DarkRed,
				DiagnosticSeverity.Warning => ConsoleColor.DarkYellow,
				_ => ConsoleColor.White
			};
			
			lock (ConsoleLock)
			{
				Console.ForegroundColor = color;
				Console.WriteLine($"------------ {relativePath} ------------");
				Console.WriteLine($"{label}:");
			}

			foreach (var diagnostic in diagnostics)
			{
				const int maxLineNumberLength = 8;
				const char lineBar = '\u2502';
				const char upArrow = '\u2191';
				const char downArrow = '\u2193';
				
				// Todo: Support multiline errors
				
				var lineHeader = diagnostic.line == 0
					? $"{"?",maxLineNumberLength}"
					: $"{diagnostic.line.ToString(),maxLineNumberLength}";

				var messageHeader = $"{new string(' ', lineHeader.Length)} {lineBar} ";
				var lineRange = diagnostic.source.GetLineRange(diagnostic.line);
				var lineText = diagnostic.source.GetText(diagnostic.line);
				lock (ConsoleLock)
				{
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write($"{lineHeader} {lineBar} ");
					Console.ForegroundColor = ConsoleColor.Gray;
					
					if (diagnostic.range is { } range)
					{
						var columnSkips = 0;
						var preRange = new TextRange(lineRange.Start, range.Start);
						while (char.IsWhiteSpace(diagnostic.source[preRange.Start]))
						{
							preRange = new TextRange(preRange.Start + 1, preRange.End);
							columnSkips++;
						}
						
						var postRange = new TextRange(range.End, lineRange.End);

						if (diagnostic.line > 0)
						{
							Console.Write(diagnostic.source.GetText(preRange));
							Console.ForegroundColor = darkColor;
							Console.Write(diagnostic.source.GetText(range));
							Console.ForegroundColor = ConsoleColor.Gray;
							Console.WriteLine(diagnostic.source.GetText(postRange));
						}
						else
						{
							Console.WriteLine();
						}

						Console.ForegroundColor = ConsoleColor.DarkGray;
						Console.Write(messageHeader);
						Console.ForegroundColor = darkColor;
						var column = diagnostic.source.GetLineColumn(range.Start).Item2 - (columnSkips + 1);
						Console.Write(new string(' ', column));
						Console.WriteLine(new string(upArrow, range.Length));
					}
					else
					{
						if (diagnostic.line > 0)
							Console.WriteLine(lineText);
						else
							Console.WriteLine();
					}

					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write(messageHeader);
					Console.ForegroundColor = color;
					Console.WriteLine(diagnostic.message);
					
					Console.ResetColor();
					Console.WriteLine();
				}
			}

			lock (ConsoleLock)
			{
				Console.ResetColor();
			}
		}
	}

	private static async Task Compile(string[] args)
	{
		var programArgs = ProgramArgs.Parse(args);
		if (programArgs is null)
		{
			await Console.Error.WriteLineAsync("Invalid arguments.");
			return;
		}
		
		if (programArgs.Value.InputPath is not { } inputPath)
		{
			await Console.Error.WriteLineAsync("Missing input path. Use the '-in <path>' argument.");
			return;
		}
		
		if (programArgs.Value.OutputPath is not { } outputPath)
		{
			await Console.Error.WriteLineAsync("Missing output path. Use the '-out <path>' argument.");
			return;
		}

		var optimizationLevel = programArgs.Value.OptimizationLevel ?? 0;

		string workingDirectory;
		var files = new List<FileDiagnostics>();

		if (File.Exists(inputPath))
		{
			workingDirectory = Path.GetDirectoryName(inputPath)!;
			files.Add(new FileDiagnostics(workingDirectory, inputPath));
		}
		else if (Directory.Exists(inputPath))
		{
			workingDirectory = inputPath;
			foreach (var file in Directory.EnumerateFiles(inputPath, "*.bs", SearchOption.AllDirectories))
				files.Add(new FileDiagnostics(workingDirectory, file));
		}

		var startTime = DateTime.Now;
		var compilationTasks = files.Select(ParseFile).ToArray();
		await Task.WhenAll(compilationTasks);

		var asts = new Ast[compilationTasks.Length];
		var i = 0;
		foreach (var task in compilationTasks)
		{
			if (task.Result is { } ast)
			{
				asts[i++] = ast;
				continue;
			}

			Console.ForegroundColor = ConsoleColor.Red;
			await Console.Error.WriteLineAsync("Compilation failed.");
			Console.ResetColor();

			return;
		}

		if (!Analyze(programArgs.Value.Target?.Is64Bit() ?? Environment.Is64BitOperatingSystem, asts, files,
			    out var resolvedAsts))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			await Console.Error.WriteLineAsync("Compilation failed.");
			Console.ResetColor();

			return;
		}

		var codeGenerator = new CodeGenerator
		{
			Debug = true
		};
		
		outputPath = codeGenerator.Generate(resolvedAsts, programArgs.Value.Target, optimizationLevel, outputPath);
		
		var duration = (DateTime.Now - startTime).TotalMilliseconds;
		Console.ForegroundColor = ConsoleColor.Cyan;
		await Console.Out.WriteLineAsync($"Compilation succeeded. ({duration} ms)");
		Console.ResetColor();

		if (!File.Exists(outputPath))
			return;

		if (Path.GetExtension(outputPath) != ".exe")
			return;
		
		var processStartInfo = new ProcessStartInfo
		{
			FileName = outputPath,
			Arguments = "",
			WindowStyle = ProcessWindowStyle.Normal
		};

		var process = new Process
		{
			StartInfo = processStartInfo
		};

		process.Start();
		await process.WaitForExitAsync();

		switch (process.ExitCode)
		{
			case 0:
				Console.ForegroundColor = ConsoleColor.Blue;
				await Console.Out.WriteLineAsync($"\nApplication finished with exit code {process.ExitCode}");
				Console.ResetColor();
				break;
			
			default:
				Console.ForegroundColor = ConsoleColor.DarkRed;
				await Console.Out.WriteLineAsync($"\nApplication finished with exit code {process.ExitCode}");
				Console.ResetColor();
				break;
		}
	}

	private static bool Analyze(bool is64Bit, IReadOnlyList<Ast> asts, IReadOnlyList<FileDiagnostics> files,
		out List<ResolvedAst> resolvedAsts)
	{
		resolvedAsts = [];
		var collector = new Collector(is64Bit);
		var collectionError = false;
		var collectedAsts = new List<CollectedAst>();

		for (var i = 0; i < asts.Count; i++)
		{
			var ast = asts[i];
			var file = files[i];
			if (collector.Collect(ast, file.workingDirectory, file.filePath) is { } collectedAst)
			{
				collectedAsts.Add(collectedAst);
				continue;
			}
			
			collectionError = true;
			
			foreach (var exception in collector.exceptions.Where(e =>
				         e.WorkingDirectory == file.workingDirectory && e.FilePath == file.filePath))
			{
				file.collectionExceptions.Add(exception);
			}
			
			file.Print();
		}

		if (collectionError)
			return false;

		for (var i = 0; i < collectedAsts.Count; i++)
		{
			var ast = collectedAsts[i];
			var file = files[i];
			collector.Collect(ast);

			foreach (var exception in collector.exceptions.Where(e =>
				         e.WorkingDirectory == file.workingDirectory && e.FilePath == file.filePath))
			{
				file.collectionExceptions.Add(exception);
			}
			
			file.Print();
		}

		if (collector.exceptions.Count > 0)
		{
			return false;
		}

		var resolver = new Resolver(collector);
		var hadError = false;

		for (var i = 0; i < collectedAsts.Count; i++)
		{
			var ast = collectedAsts[i];
			var file = files[i];

			try
			{
				if (resolver.Resolve(ast) is { } resolvedAst)
					resolvedAsts.Add(resolvedAst);
			}
			catch (Exception e)
			{
				hadError = true;
				
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine($"{e.Message}\n{e.StackTrace}");
				Console.ResetColor();
			}

			foreach (var exception in resolver.exceptions.Where(e =>
				         e.WorkingDirectory == file.workingDirectory && e.FilePath == file.filePath))
			{
				file.resolutionExceptions.Add(exception);
			}

			file.Print();
		}

		resolver.Verify();

		if (hadError || resolver.exceptions.Count > 0)
		{
			return false;
		}

		return true;
	}

	private static async Task<Ast?> ParseFile(FileDiagnostics file)
	{
		var source = await File.ReadAllTextAsync(file.filePath);
		var lexer = new FilteredLexer(new StringBuffer(source));
		var parser = new Parser();
		var ast = parser.Parse(lexer, out var parseDiagnostics);
		
		if (parseDiagnostics.Count <= 0)
			return ast;

		file.parseExceptions.Add(parseDiagnostics);
		file.Print();

		return ast;
	}

	private static void Interpret(string source)
	{
		var lexer = new FilteredLexer(new StringBuffer(source));

		foreach (var token in lexer)
		{
			PrintLine($"\t{token}");
		}
	}
}