using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

internal class ParseException : Exception
{
	public ParseException(string? message, Token? token = null)
	: base(FormatMessage(message, token))
	{
	}

	private static string? FormatMessage(string? message, Token? token)
	{
		if (message is null)
			return null;

		if (token is null)
			return message;

		
		return $"[{token.Line}:{token.Column}] {message}";
	}
}

public static class Parser
{
	public static Ast? Parse(ILexer lexer)
	{
		var tokens = new List<Token>();
		
		foreach (var token in lexer)
			tokens.Add(token);
		
		var root = ParseProgram(tokens, 0);
		if (root is not null)
			return new Ast(root);

		return null;
	}

	private static bool IsEndOfFile(IReadOnlyCollection<Token> tokens, int position) => position >= tokens.Count;

	private static bool Match(IReadOnlyList<Token> tokens, ref int position, params TokenType[] types)
	{
		if (IsEndOfFile(tokens, position))
			return types.Contains(TokenType.EndOfFile);
		
		var token = tokens[position];
		
		if (!types.Contains(token.Type))
			return false;
		
		position++;
		return true;
	}

	private static TokenType Peek(IReadOnlyList<Token> tokens, int position)
	{
		return TokenAt(tokens, position)?.Type ?? TokenType.EndOfFile;
	}

	private static Token? TokenAt(IReadOnlyList<Token> tokens, int position)
	{
		return IsEndOfFile(tokens, position) ? null : tokens[position];
	}

	private static Token Consume(IReadOnlyList<Token> tokens, ref int position, string? message, params TokenType[] types)
	{
		if (message is null)
		{
			var typeString = new StringBuilder();

			if (types.Length == 1)
				typeString.Append($"'{types[0]}'");
			else
			{
				typeString.Append("one of: ");
				typeString.AppendJoin(',', types.Select(t => $"'{t}'"));
			}

			message = $"Expected {typeString}";
		}
		
		if (IsEndOfFile(tokens, position))
			throw new ParseException($"{message}; Instead, got 'end of file'", tokens.FirstOrDefault());

		var token = tokens[position];
		if (!types.Contains(token.Type))
			throw new ParseException($"{message}; Instead, got '{token.Type}'", token);
		
		position++;
		return token;
	}

	private static ProgramStatement? ParseProgram(IReadOnlyList<Token> tokens, int position)
	{
		try
		{
			var imports = ParseImportStatements(tokens, ref position);
			TryParseModuleStatement(tokens, ref position, false, out var module);
			var statements = ParseTopLevelStatements(tokens, ref position);

			if (Peek(tokens, position) != TokenType.EndOfFile)
				throw new ParseException("Expected 'end of file'", TokenAt(tokens, position));

			return new ProgramStatement(imports, module, statements);
		}
		catch (ParseException e)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(e.Message);
			Console.ResetColor();
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static List<ImportStatement> ParseImportStatements(IReadOnlyList<Token> tokens, ref int position)
	{
		var imports = new List<ImportStatement>();
		while (TryParseImportStatement(tokens, ref position, out var import))
			imports.Add(import);

		return imports;
	}

	private static bool TryParseImportStatement(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out ImportStatement? import)
	{
		import = null;
		
		if (!Match(tokens, ref position, TokenType.KeywordImport))
			return false;

		var identifierTokens = new List<Token>();
		
		do
		{
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier, TokenType.OpStar);
			identifierTokens.Add(identifier);
		} while (Match(tokens, ref position, TokenType.OpDot));

		if (identifierTokens.Count < 2)
			throw new ParseException("Invalid import statement", identifierTokens.LastOrDefault());

		var scope = identifierTokens.Take(identifierTokens.Count - 1).ToImmutableArray();
		var importToken = identifierTokens.Last();

		foreach (var token in scope)
		{
			if (token.Type != TokenType.Identifier)
				throw new ParseException("Invalid import statement: Expected 'identifier'", token);
		}

		import = new ImportStatement(scope, importToken);
		return true;
	}

	private static bool TryParseModuleStatement(IReadOnlyList<Token> tokens, ref int position, bool requireBody,
		[NotNullWhen(true)] out ModuleStatement? module)
	{
		module = null;
		if (Peek(tokens, position) != TokenType.KeywordModule)
			return false;

		module = ParseModuleStatement(tokens, ref position, requireBody);
		return true;
	}
		
	private static ModuleStatement ParseModuleStatement(IReadOnlyList<Token> tokens, ref int position, bool requireBody)
	{
		Consume(tokens, ref position, null, TokenType.KeywordModule);
		
		var scope = new List<Token>();
		
		do
		{
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			scope.Add(identifier);
		} while (Match(tokens, ref position, TokenType.OpDot));

		foreach (var token in scope)
		{
			if (token.Type != TokenType.Identifier)
				throw new ParseException("Invalid module statement: Expected 'identifier'", token);
		}

		var statements = new List<StatementNode>();
		if (Match(tokens, ref position, TokenType.OpLeftBrace))
		{
			statements.AddRange(ParseTopLevelStatements(tokens, ref position));
			Consume(tokens, ref position, "Expected statement", TokenType.OpRightBrace);
		}
		else if (requireBody)
			throw new ParseException("Module statement must have a body", TokenAt(tokens, position));

		return new ModuleStatement(scope, statements);
	}

	private static List<StatementNode> ParseTopLevelStatements(IReadOnlyList<Token> tokens, ref int position)
	{
		var statements = new List<StatementNode>();

		while (true)
		{
			if (TryParseModuleStatement(tokens, ref position, true, out var moduleStatement))
			{
				statements.Add(moduleStatement);
				continue;
			}

			break;
		}
		
		return statements;
	}
}