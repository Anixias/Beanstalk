using System.Text;
using Beanstalk.Analysis.Semantics;
using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;
using Beanstalk.CodeGen;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Compiler;

internal struct ProgramArgs
{
	public string? InputPath { get; private set; }
	public string? OutputPath { get; private set; }
	public int? OptimizationLevel { get; private set; }
	public int? BitTarget { get; private set; }

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
				case "-bit":
					programArgs.BitTarget = int.Parse(Read());
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
		public readonly List<ParseException> parseExceptions = [];
		public readonly List<CollectionException> collectionExceptions = [];

		public FileDiagnostics(string workingDirectory, string filePath)
		{
			this.workingDirectory = workingDirectory;
			this.filePath = filePath;
		}

		public void Print()
		{
			if (parseExceptions.Count > 0)
				PrintErrorList("Parsing Error(s)", parseExceptions);
			
			if (collectionExceptions.Count > 0)
				PrintErrorList("Collection Error(s)", collectionExceptions);
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
		var is64Bit = true;

		switch (programArgs.Value.BitTarget)
		{
			case 64:
			case null:
				break;
			case 32:
				is64Bit = false;
				break;
			default:
				await Console.Error.WriteLineAsync("Invalid target architecture. Use '-bit 32' or '-bit 64').");
				return;
		}

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

		if (!Analyze(is64Bit, asts, files))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			await Console.Error.WriteLineAsync("Compilation failed.");
			Console.ResetColor();

			return;
		}
		
		GenerateIR(asts);
		
		var duration = (DateTime.Now - startTime).TotalMilliseconds;
		Console.ForegroundColor = ConsoleColor.Cyan;
		await Console.Out.WriteLineAsync($"Compilation succeeded. ({duration} ms)");
		Console.ResetColor();
	}

	private static void GenerateIR(Ast[] asts)
	{
		
	}

	private static bool Analyze(bool is64Bit, IReadOnlyList<Ast> asts, IReadOnlyList<FileDiagnostics> files)
	{
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
		return true;
	}

	private static async Task<Ast?> ParseFile(FileDiagnostics file)
	{
		var source = await File.ReadAllTextAsync(file.filePath);
		var lexer = new FilteredLexer(new StringBuffer(source));
		var ast = Parser.Parse(lexer, out var parseDiagnostics);
		
		if (parseDiagnostics.Count <= 0)
			return ast;

		file.parseExceptions.AddRange(parseDiagnostics);
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