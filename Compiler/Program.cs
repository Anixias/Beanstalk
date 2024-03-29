﻿using System.Text;
using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Compiler;

internal struct ProgramArgs
{
	public string? InputPath { get; private set; }
	public string? OutputPath { get; private set; }
	public int? OptimizationLevel { get; private set; }

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

		var files = new List<string>();

		if (File.Exists(inputPath))
		{
			files.Add(inputPath);
		}
		else if (Directory.Exists(inputPath))
		{
			foreach (var file in Directory.EnumerateFiles(inputPath, "*.bs", SearchOption.AllDirectories))
				files.Add(file);
		}

		var compilationTasks = files.Select(CompileFile).ToArray();
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

			await Console.Error.WriteLineAsync("Compilation failed.");
			return;
		}

		Analyze(asts);
		GenerateIR(asts);
	}

	private static void GenerateIR(Ast[] asts)
	{
		
	}

	private static void Analyze(Ast[] asts)
	{
		
	}

	private static async Task<Ast?> CompileFile(string file)
	{
		PrintLine($"------------ {file} ------------");
		var source = await File.ReadAllTextAsync(file);
		var lexer = new FilteredLexer(new StringBuffer(source));
		var ast = Parser.Parse(lexer);

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