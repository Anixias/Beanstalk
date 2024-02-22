﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Beanstalk.Analysis.Diagnostics;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public class ParseException : Exception
{
	public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Error;
	private readonly IBuffer source;
	private readonly TextRange range;

	public ParseException(string? message, Token token, TextRange? range = null)
	: base(message)
	{
		source = token.Source;
		this.range = range ?? token.Range;
	}

	public ParseException(string? message, IBuffer source, TextRange range)
	: base(message)
	{
		this.source = source;
		this.range = range;
	}
	
	public static implicit operator Diagnostic(ParseException parseException)
	{
		return new Diagnostic(parseException.Severity, parseException.source, parseException.range,
			parseException.Message);
	}
}

public sealed class Parser
{
	private IBuffer source = null!;
	private DiagnosticList currentDiagnostics = null!;
	
	public Ast? Parse(ILexer lexer, out DiagnosticList diagnostics)
	{
		source = lexer.Source;
		try
		{
			var tokens = new List<Token>();

			currentDiagnostics = new DiagnosticList();
			foreach (var token in lexer)
			{
				tokens.Add(token);

				if (!token.Type.IsInvalid)
					continue;

				if (token.Type != TokenType.Invalid)
				{
					var name = token.Type.ToString();
					name = name[0].ToString().ToUpper() + name[1..];
					currentDiagnostics.Add(new ParseException(name, token));
					continue;
				}

				var plural = token.Text.Length > 1 ? "characters" : "character";
				currentDiagnostics.Add(new ParseException($"Unexpected {plural}", token));
			}

			if (ParseProgram(tokens, 0) is { } root)
				return new Ast(root, lexer.Source);

			return null;
		}
		finally
		{
			diagnostics = currentDiagnostics;
			source = null!;
			currentDiagnostics = null!;
		}
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

	private static bool Match(IReadOnlyList<Token> tokens, ref int position, [NotNullWhen(true)] out Token? token,
		params TokenType[] types)
	{
		if (types.Contains(TokenType.EndOfFile))
			throw new ArgumentException("Cannot match EndOfFile token", nameof(types));
		
		token = null;
		
		if (IsEndOfFile(tokens, position))
			return false;
		
		token = tokens[position];
		
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

	private Token Consume(IReadOnlyList<Token> tokens, ref int position, string? message,
		params TokenType[] types)
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
		{
			throw tokens.Count > 0
				? new ParseException($"{message}; Instead, got 'end of file'", tokens.Last())
				: new ParseException($"{message}; Instead, got 'end of file'", source, TextRange.Empty);
		}

		var token = tokens[position];
		if (!types.Contains(token.Type))
			throw new ParseException($"{message}; Instead, got '{token.Type}'", token);
		
		position++;
		return token;
	}

	private ProgramStatement? ParseProgram(IReadOnlyList<Token> tokens, int position)
	{
		try
		{
			var startToken = TokenAt(tokens, position);
			var imports = ParseImportStatements(tokens, ref position);

			ModuleStatement? module = null;
			if (Peek(tokens, position) == TokenType.KeywordModule)
				module = ParseModuleStatement(tokens, ref position, false);
			
			var statements = ParseTopLevelStatements(tokens, ref position);

			if (Peek(tokens, position) != TokenType.EndOfFile)
			{
				if (TokenAt(tokens, position) is { } token)
					currentDiagnostics.Add(new ParseException("Expected 'end of file'", token));
				else
					currentDiagnostics.Add(new ParseException("Expected 'end of file'", source, TextRange.Empty));
			}

			if (currentDiagnostics.ErrorCount > 0)
				return null;

			var range = startToken is null
				? new TextRange(0, 0)
				: new TextRange(0, startToken.Source.Length - 1);

			return new ProgramStatement(imports, module, statements, range);
		}
		catch (ParseException e)
		{
			currentDiagnostics.Add(e);
			return null;
		}
		catch
		{
			return null;
		}
	}

	private List<StatementNode> ParseImportStatements(IReadOnlyList<Token> tokens, ref int position)
	{
		var imports = new List<StatementNode>();
		while (TryParseImportStatement(tokens, ref position, out var import))
			imports.Add(import);

		return imports;
	}

	private bool TryParseImportStatement(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out StatementNode? import)
	{
		import = null;
		
		if (!Match(tokens, ref position, out var startToken, TokenType.KeywordImport))
			return false;

		var identifierTokens = new List<Token>();
		var importTokens = new List<ImportToken>();
		var isAggregate = false;
		var range = startToken.Range;
		
		do
		{
			if (Match(tokens, ref position, out var identifier, TokenType.Identifier, TokenType.OpStar))
			{
				identifierTokens.Add(identifier);
				range = range.Join(identifier.Range);
			}
			else if (Match(tokens, ref position, TokenType.OpLeftBrace))
			{
				do
				{
					var identifierToken = Consume(tokens, ref position, null, TokenType.Identifier);
					Token? tokenAlias = null;
					if (Match(tokens, ref position, TokenType.KeywordAs))
					{
						tokenAlias = Consume(tokens, ref position, null, TokenType.Identifier);
					}
					
					importTokens.Add(new ImportToken(identifierToken, tokenAlias));
				} while (Match(tokens, ref position, TokenType.OpComma));
				
				var endToken = Consume(tokens, ref position, null, TokenType.OpRightBrace);
				range = range.Join(endToken.Range);
				isAggregate = true;
				break;
			}
		} while (Match(tokens, ref position, TokenType.OpDot));

		if (identifierTokens.Count < (isAggregate ? 1 : 2))
		{
			if (identifierTokens.LastOrDefault() is { } token)
				throw new ParseException("Invalid import statement", token);
			
			throw new ParseException("Invalid import statement", source, TextRange.Empty);
		}

		var scope = identifierTokens.Take(identifierTokens.Count - 1).ToImmutableArray();

		foreach (var token in scope)
		{
			if (token.Type != TokenType.Identifier)
				throw new ParseException("Invalid import statement: Expected 'identifier'", token);
		}

		Token? alias = null;
		if (Match(tokens, ref position, TokenType.KeywordAs))
		{
			alias = Consume(tokens, ref position, null, TokenType.Identifier);
			range = range.Join(alias.Range);
		}

		var moduleName = new ModuleName(scope);
		if (!isAggregate)
		{
			import = new ImportStatement(moduleName, identifierTokens.Last(), alias, range);
		}
		else
		{
			import = new AggregateImportStatement(moduleName, importTokens, alias, range);
		}

		return true;
	}

	private ModuleStatement ParseModuleStatement(IReadOnlyList<Token> tokens, ref int position, bool requireBody)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordModule);
		
		var scope = new List<Token>();
		var range = startToken.Range;
		do
		{
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			scope.Add(identifier);
			range = range.Join(identifier.Range);
		} while (Match(tokens, ref position, TokenType.OpDot));

		foreach (var token in scope)
		{
			if (token.Type != TokenType.Identifier)
				currentDiagnostics.Add(new ParseException("Invalid module statement: Expected 'identifier'", token));
		}

		var statements = new List<StatementNode>();
		if (Match(tokens, ref position, TokenType.OpLeftBrace))
		{
			statements.AddRange(ParseTopLevelStatements(tokens, ref position, TokenType.OpRightBrace));
			var endToken = Consume(tokens, ref position, "Expected statement", TokenType.OpRightBrace);
			range = range.Join(endToken.Range);
		}
		else if (requireBody)
		{
			if (TokenAt(tokens, position - 1) is { } token)
				currentDiagnostics.Add(new ParseException("Module statement must have a body", token));
			else
				currentDiagnostics.Add(new ParseException("Module statement must have a body", source, TextRange.Empty));
		}

		return new ModuleStatement(new ModuleName(scope), statements, range);
	}

	private bool TryParseEntryStatement(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out EntryStatement? entry)
	{
		entry = null;
		if (Peek(tokens, position) != TokenType.KeywordEntry)
			return false;

		entry = ParseEntryStatement(tokens, ref position);
		return true;
	}

	private EntryStatement ParseEntryStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordEntry);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var parameters = new List<Parameter>();
		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			do
			{
				parameters.Add(ParseParameter(tokens, ref position, false));
			} while (Match(tokens, ref position, TokenType.OpComma));
		}
		
		Consume(tokens, ref position, null, TokenType.OpRightParen);

		var body = ParseBlockStatement(tokens, ref position);
		return new EntryStatement(parameters, body, startToken.Range.Join(body.range));
	}

	private bool TryParseDefineStatement(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out DefineStatement? defineStatement)
	{
		defineStatement = null;
		if (Peek(tokens, position) != TokenType.KeywordEntry)
			return false;

		defineStatement = ParseDefineStatement(tokens, ref position);
		return true;
	}

	private DefineStatement ParseDefineStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordDef);
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		Consume(tokens, ref position, null, TokenType.KeywordAs);
		var type = ParseType(tokens, ref position);
		
		return new DefineStatement(identifier, type, startToken.Range.Join(type.range));
	}

	private bool TryParseDllImportStatement(IReadOnlyList<Token> tokens, ref int position, [NotNullWhen(true)] out DllImportStatement? dllImportStatement)
	{
		dllImportStatement = null;
		if (Peek(tokens, position) != TokenType.KeywordImport)
			return false;

		dllImportStatement = ParseDllImportStatement(tokens, ref position);
		return true;
	}

	private DllImportStatement ParseDllImportStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordImport);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		var dllPath = Consume(tokens, ref position, null, TokenType.StringLiteral).Value as string ?? "";
		Consume(tokens, ref position, null, TokenType.OpRightParen);

		var statements = new List<StatementNode>();
		var range = startToken.Range;
		if (Match(tokens, ref position, TokenType.OpLeftBrace))
		{
			statements.AddRange(ParseDllImportedStatements(tokens, ref position, TokenType.OpRightBrace));
			var endToken = Consume(tokens, ref position, null, TokenType.OpRightBrace);
			range = range.Join(endToken.Range);
		}
		else
		{
			var statement = ParseDllImportedStatement(tokens, ref position);
			range = range.Join(statement.range);
		}
		
		return new DllImportStatement(dllPath, statements, range);
	}

	private bool TryParseFunctionDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out FunctionDeclarationStatement? functionDeclaration)
	{
		functionDeclaration = null;

		var startPosition = position;
		var oldDiagnostics = currentDiagnostics;
		currentDiagnostics = new DiagnosticList();
		
		try
		{
			functionDeclaration = ParseFunctionDeclaration(tokens, ref position);
			oldDiagnostics.Add(currentDiagnostics);
			currentDiagnostics = oldDiagnostics;
			return true;
		}
		catch (ParseException)
		{
			position = startPosition;
			return false;
		}
		finally
		{
			oldDiagnostics.Add(currentDiagnostics);
			currentDiagnostics = oldDiagnostics;
		}
	}

	private FunctionDeclarationStatement ParseFunctionDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		Token? startToken = null;

		// Todo: Access/visibility modifiers
		var isStatic = false;
		var isPure = true;

		while (Peek(tokens, position) != TokenType.KeywordFun)
		{
			var modifier = Consume(tokens, ref position, null, TokenType.KeywordStatic, TokenType.KeywordVar);
			startToken ??= modifier;

			if (modifier.Type == TokenType.KeywordStatic)
			{
				if (isStatic)
					currentDiagnostics.Add(new ParseException("Function is already marked as static", modifier));

				isStatic = true;
			}
			else if (modifier.Type == TokenType.KeywordVar)
			{
				if (!isPure)
					currentDiagnostics.Add(new ParseException("Function is already marked as impure", modifier));

				isPure = false;
			}
		}

		var funKeyword = Consume(tokens, ref position, null, TokenType.KeywordFun);
		startToken ??= funKeyword;
		var signatureRange = startToken.Range;
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);

		var typeParameters = new List<Token>();
		if (Match(tokens, ref position, TokenType.OpLeftBracket))
		{
			do
			{
				typeParameters.Add(Consume(tokens, ref position, null, TokenType.Identifier));
			} while (Match(tokens, ref position, TokenType.OpComma));
			Consume(tokens, ref position, null, TokenType.OpRightBracket);
		}
		
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var parameters = new List<Parameter>();
		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			do
			{
				parameters.Add(ParseParameter(tokens, ref position, true));
			} while (Match(tokens, ref position, TokenType.OpComma));
		}
		
		var rightParenToken = Consume(tokens, ref position, null, TokenType.OpRightParen);
		signatureRange = signatureRange.Join(rightParenToken.Range);

		SyntaxType? returnType = null;
		if (Match(tokens, ref position, TokenType.OpReturnType))
		{
			returnType = ParseType(tokens, ref position);
			signatureRange = signatureRange.Join(returnType.range);
		}

		StatementNode body;
		if (Match(tokens, ref position, TokenType.OpDoubleArrow))
		{
			var expression = ParseExpression(tokens, ref position);
			body = new ReturnStatement(expression, expression.range);
		}
		else
		{
			body = ParseBlockStatement(tokens, ref position);
		}

		return new FunctionDeclarationStatement(identifier, isStatic, isPure, typeParameters, parameters, returnType,
			body, startToken.Range.Join(body.range), signatureRange);
	}

	private bool TryParseExternalFunctionStatement(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out ExternalFunctionStatement? externalFunctionStatement)
	{
		externalFunctionStatement = null;
		var startPosition = position;
		var oldDiagnostics = currentDiagnostics;
		currentDiagnostics = new DiagnosticList();

		try
		{
			externalFunctionStatement = ParseExternalFunctionStatement(tokens, ref position);
			return true;
		}
		catch (ParseException)
		{
			position = startPosition;
			return false;
		}
		finally
		{
			oldDiagnostics.Add(currentDiagnostics);
			currentDiagnostics = oldDiagnostics;
		}
	}

	private ExternalFunctionStatement ParseExternalFunctionStatement(IReadOnlyList<Token> tokens,
		ref int position)
	{
		Token? startToken = null;

		// Todo: Access/visibility modifiers
		var isStatic = false;
		var isPure = true;

		while (Peek(tokens, position) != TokenType.KeywordFun)
		{
			var modifier = Consume(tokens, ref position, null, TokenType.KeywordStatic, TokenType.KeywordVar);
			startToken ??= modifier;

			if (modifier.Type == TokenType.KeywordStatic)
			{
				if (isStatic)
					currentDiagnostics.Add(new ParseException("Function is already marked as static", modifier));

				isStatic = true;
			}
			else if (modifier.Type == TokenType.KeywordVar)
			{
				if (!isPure)
					currentDiagnostics.Add(new ParseException("Function is already marked as impure", modifier));

				isPure = false;
			}
		}

		var funKeyword = Consume(tokens, ref position, null, TokenType.KeywordFun);
		startToken ??= funKeyword;
		var signatureRange = startToken.Range;
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var parameters = new List<Parameter>();
		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			do
			{
				parameters.Add(ParseParameter(tokens, ref position, false));
			} while (Match(tokens, ref position, TokenType.OpComma));
		}
		
		var rightParenToken = Consume(tokens, ref position, null, TokenType.OpRightParen);
		signatureRange = signatureRange.Join(rightParenToken.Range);

		SyntaxType? returnType = null;
		if (Match(tokens, ref position, TokenType.OpReturnType))
		{
			returnType = ParseType(tokens, ref position);
			signatureRange = signatureRange.Join(returnType.range);
		}

		var attributes = new Dictionary<string, string>();
		Consume(tokens, ref position, null, TokenType.OpDoubleArrow);
		Consume(tokens, ref position, null, TokenType.KeywordExternal);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			do
			{
				// Todo: This will be a list of allowed keys; currently only 'entry' is defined
				var attributeKey = Consume(tokens, ref position, null, TokenType.KeywordEntry);
				Consume(tokens, ref position, null, TokenType.OpEquals);
				var attributeValue = Consume(tokens, ref position, null, TokenType.StringLiteral);

				if (!attributes.TryAdd(attributeKey.Text, (string)attributeValue.Value!))
					throw new ParseException(
						$"Attribute '{attributeKey.Text}' is already defined for this function signature",
						attributeKey);
			} while (Match(tokens, ref position, TokenType.OpComma));
		}

		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);
		return new ExternalFunctionStatement(identifier, parameters, returnType, attributes,
			startToken.Range.Join(endToken.Range), signatureRange);
	}

	private bool TryParseConstructorDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out ConstructorDeclarationStatement? constructorDeclaration)
	{
		constructorDeclaration = null;
		if (Peek(tokens, position) != TokenType.KeywordConstructor)
			return false;

		constructorDeclaration = ParseConstructorDeclaration(tokens, ref position);
		return true;
	}

	private ConstructorDeclarationStatement ParseConstructorDeclaration(IReadOnlyList<Token> tokens,
		ref int position)
	{
		var constructorKeyword = Consume(tokens, ref position, null, TokenType.KeywordConstructor);
		
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var parameters = new List<Parameter>();
		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			do
			{
				parameters.Add(ParseParameter(tokens, ref position, true));
			} while (Match(tokens, ref position, TokenType.OpComma));
		}
		
		Consume(tokens, ref position, null, TokenType.OpRightParen);

		var body = ParseBlockStatement(tokens, ref position);

		return new ConstructorDeclarationStatement(constructorKeyword, parameters, body,
			constructorKeyword.Range.Join(body.range));
	}

	private bool TryParseDestructorDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out DestructorDeclarationStatement? destructorDeclaration)
	{
		destructorDeclaration = null;
		if (Peek(tokens, position) != TokenType.KeywordDestructor)
			return false;

		destructorDeclaration = ParseDestructorDeclaration(tokens, ref position);
		return true;
	}

	private DestructorDeclarationStatement ParseDestructorDeclaration(IReadOnlyList<Token> tokens,
		ref int position)
	{
		var destructorKeyword = Consume(tokens, ref position, null, TokenType.KeywordDestructor);
		
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		Consume(tokens, ref position, null, TokenType.OpRightParen);

		var body = ParseBlockStatement(tokens, ref position);

		return new DestructorDeclarationStatement(destructorKeyword, body, destructorKeyword.Range.Join(body.range));
	}

	private bool TryParseFieldDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out FieldDeclarationStatement? fieldDeclaration)
	{
		fieldDeclaration = null;
		
		var modifierTokens = new[]
		{
			TokenType.KeywordStatic,
			TokenType.KeywordConst,
			TokenType.KeywordVar
		};
		
		var peek = Peek(tokens, position);
		var lookahead = 0;
		while (modifierTokens.Contains(peek))
		{
			peek = Peek(tokens, position + ++lookahead);
		}
		
		if (peek != TokenType.Identifier)
			return false;

		fieldDeclaration = ParseFieldDeclaration(tokens, ref position);
		return true;
	}

	private FieldDeclarationStatement ParseFieldDeclaration(IReadOnlyList<Token> tokens,
		ref int position)
	{
		var startToken = TokenAt(tokens, position)!;
		
		var isMutable = false;
		var isConst = false;
		var isStatic = false;

		var peek = Peek(tokens, position);
		while (peek != TokenType.Identifier)
		{
			var modifierToken = TokenAt(tokens, position++);

			const string duplicateErrorMessage = "Duplicate field modifier";
			const string mutableConstErrorMessage = "Field cannot be both mutable and constant";

			if (peek == TokenType.KeywordVar)
			{
				if (isMutable)
					throw modifierToken is not null
						? new ParseException(duplicateErrorMessage, modifierToken)
						: new ParseException(duplicateErrorMessage, source, TextRange.Empty);
				
				if (isConst)
					throw modifierToken is not null
						? new ParseException(mutableConstErrorMessage, modifierToken)
						: new ParseException(mutableConstErrorMessage, source, TextRange.Empty);
				
				isMutable = true;
				peek = Peek(tokens, position);
				continue;
			}

			if (peek == TokenType.KeywordConst)
			{
				if (isConst)
					throw modifierToken is not null
						? new ParseException(duplicateErrorMessage, modifierToken)
						: new ParseException(duplicateErrorMessage, source, TextRange.Empty);
				
				if (isMutable)
					throw modifierToken is not null
						? new ParseException(mutableConstErrorMessage, modifierToken)
						: new ParseException(mutableConstErrorMessage, source, TextRange.Empty);
				
				isConst = true;
				peek = Peek(tokens, position);
				continue;
			}

			const string invalidFieldModifierErrorMessage = "Invalid field modifier";
			if (peek != TokenType.KeywordStatic)
				throw modifierToken is not null
					? new ParseException(invalidFieldModifierErrorMessage, modifierToken)
					: new ParseException(invalidFieldModifierErrorMessage, source, TextRange.Empty);
			
			if (isStatic)
				throw modifierToken is not null
					? new ParseException(duplicateErrorMessage, modifierToken)
					: new ParseException(duplicateErrorMessage, source, TextRange.Empty);
				
			isStatic = true;
			peek = Peek(tokens, position);
		}
		
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);

		// A type is required for fields - this may change in the future, allowing field types to be inferred from
		// the initializer. The main reason not to do so is if the initializer refers to a member, it may require
		// several passes to fully infer the type
		Consume(tokens, ref position, null, TokenType.OpColon);
		var type = ParseType(tokens, ref position);
		var range = startToken.Range.Join(type.range);

		ExpressionNode? initializer = null;
		if (Match(tokens, ref position, TokenType.OpEquals))
		{
			initializer = ParseExpression(tokens, ref position);
			range = startToken.Range.Join(initializer.range);
		}

		var mutability = FieldDeclarationStatement.Mutability.Immutable;
		
		if (isMutable)
			mutability = FieldDeclarationStatement.Mutability.Mutable;

		if (isConst)
			mutability = FieldDeclarationStatement.Mutability.Constant;

		return new FieldDeclarationStatement(identifier, mutability, isStatic, type, initializer, range);
	}

	private bool TryParseCastDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out CastDeclarationStatement? castDeclaration)
	{
		var validEntryTokens = new[] { TokenType.KeywordImplicit, TokenType.KeywordExplicit };
		
		castDeclaration = null;
		if (!validEntryTokens.Contains(Peek(tokens, position)))
			return false;

		castDeclaration = ParseCastDeclaration(tokens, ref position);
		return true;
	}

	private CastDeclarationStatement ParseCastDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var castTypeToken = Consume(tokens, ref position, null, TokenType.KeywordImplicit, TokenType.KeywordExplicit);
		var castKeyword = Consume(tokens, ref position, null, TokenType.KeywordCast);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		var parameter = ParseParameter(tokens, ref position, false);
		Consume(tokens, ref position, null, TokenType.OpRightParen);
		Consume(tokens, ref position, null, TokenType.OpReturnType);
		var returnType = ParseType(tokens, ref position);

		StatementNode body;

		if (Match(tokens, ref position, TokenType.OpDoubleArrow))
		{
			var expression = ParseExpression(tokens, ref position);
			body = new ReturnStatement(expression, expression.range);
		}
		else
		{
			body = ParseBlockStatement(tokens, ref position);
		}

		return new CastDeclarationStatement(castKeyword, castTypeToken.Type == TokenType.KeywordImplicit, parameter,
			returnType, body, castTypeToken.Range.Join(body.range));
	}

	private bool TryParseStringDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out StringDeclarationStatement? stringDeclaration)
	{
		var validEntryTokens = new[] { TokenType.KeywordString };
		
		stringDeclaration = null;
		if (!validEntryTokens.Contains(Peek(tokens, position)))
			return false;

		stringDeclaration = ParseStringDeclaration(tokens, ref position);
		return true;
	}

	private StringDeclarationStatement ParseStringDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var stringKeyword = Consume(tokens, ref position, null, TokenType.KeywordString);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		Consume(tokens, ref position, null, TokenType.OpRightParen);
		if (Match(tokens, ref position, TokenType.OpReturnType))
		{
			var returnType = ParseType(tokens, ref position);
			if (returnType is not BaseSyntaxType baseType || baseType.token.Type != TokenType.KeywordString)
				throw new ParseException("Return type of the string function must be 'string'", source,
					returnType.range);
		}

		StatementNode body;

		if (Match(tokens, ref position, TokenType.OpDoubleArrow))
		{
			var expression = ParseExpression(tokens, ref position);
			body = new ReturnStatement(expression, expression.range);
		}
		else
		{
			body = ParseBlockStatement(tokens, ref position);
		}

		return new StringDeclarationStatement(stringKeyword, body, stringKeyword.Range.Join(body.range));
	}

	private bool TryParseStructDeclaration(IReadOnlyList<Token> tokens, ref int position, [NotNullWhen(true)] out StructDeclarationStatement? structDeclaration)
	{
		var peek = Peek(tokens, position);

		if (peek == TokenType.KeywordStruct ||
		    (peek == TokenType.KeywordVar && Peek(tokens, position + 1) == TokenType.KeywordStruct))
		{
			structDeclaration = ParseStructDeclaration(tokens, ref position);
			return true;
		}

		structDeclaration = null;
		return false;
	}

	private StructDeclarationStatement ParseStructDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordVar, TokenType.KeywordStruct);
		var isMutable = startToken.Type == TokenType.KeywordVar;

		if (isMutable)
			Consume(tokens, ref position, null, TokenType.KeywordStruct);
		
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		Consume(tokens, ref position, null, TokenType.OpLeftBrace);
		var statements = ParseStructBody(tokens, ref position, TokenType.OpRightBrace);
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBrace);

		return new StructDeclarationStatement(identifier, isMutable, statements, startToken.Range.Join(endToken.Range));
	}

	private List<StatementNode> ParseStructBody(IReadOnlyList<Token> tokens, ref int position, TokenType endTokenType)
	{
		var syncTokens = new[]
		{
			TokenType.EndOfFile,
			TokenType.KeywordFun,
			TokenType.KeywordVar,
			TokenType.KeywordStatic,
			TokenType.OpRightBrace
		};
		
		var statements = new List<StatementNode>();

		while (Peek(tokens, position) != TokenType.EndOfFile && Peek(tokens, position) != endTokenType)
		{
			try
			{
				statements.Add(ParseStructMember(tokens, ref position));
			}
			catch (ParseException e)
			{
				currentDiagnostics.Add(e);

				do
				{
					position++;
				} while (!syncTokens.Contains(Peek(tokens, position)));
			}
		}

		return statements;
	}

	private StatementNode ParseStructMember(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TryParseFunctionDeclaration(tokens, ref position, out var functionDeclaration))
			return functionDeclaration;
		
		if (TryParseConstructorDeclaration(tokens, ref position, out var constructorDeclaration))
			return constructorDeclaration;
		
		if (TryParseDestructorDeclaration(tokens, ref position, out var destructorDeclaration))
			return destructorDeclaration;
		
		if (TryParseCastDeclaration(tokens, ref position, out var castDeclaration))
			return castDeclaration;
		
		if (TryParseStringDeclaration(tokens, ref position, out var stringDeclaration))
			return stringDeclaration;
		
		if (TryParseOperatorDeclaration(tokens, ref position, out var operationDeclaration))
			return operationDeclaration;

		if (TryParseFieldDeclaration(tokens, ref position, out var fieldDeclaration))
			return fieldDeclaration;

		if (TokenAt(tokens, position) is { } token)
			throw new ParseException("Expected struct member", token);
		
		throw new ParseException("Expected struct member", source, TextRange.Empty);
	}

	private bool TryParseOperatorDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out OperatorDeclarationStatement? operatorDeclaration)
	{
		operatorDeclaration = null;
		if (Peek(tokens, position) != TokenType.KeywordOperator)
			return false;

		operatorDeclaration = ParseOperatorDeclaration(tokens, ref position);
		return true;
	}

	private OperatorDeclarationStatement ParseOperatorDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var operatorKeyword = Consume(tokens, ref position, null, TokenType.KeywordOperator);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		var operationExpression = ParseOperationExpression(tokens, ref position);

		if (operationExpression is PrimaryOperationExpression)
			throw new ParseException("Operator required in operation expression",
				operationExpression.op ?? operatorKeyword);
		
		Consume(tokens, ref position, null, TokenType.OpRightParen);
		Consume(tokens, ref position, null, TokenType.OpReturnType);
		var returnType = ParseType(tokens, ref position);

		StatementNode body;

		if (Match(tokens, ref position, TokenType.OpDoubleArrow))
		{
			var expression = ParseExpression(tokens, ref position);
			body = new ReturnStatement(expression, expression.range);
		}
		else
		{
			body = ParseBlockStatement(tokens, ref position);
		}

		return new OperatorDeclarationStatement(operatorKeyword, operationExpression, returnType, body,
			operatorKeyword.Range.Join(body.range));
	}

	private OperationExpression ParseOperationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		return ParseBinaryOperationExpression(tokens, ref position);
	}
	
	private OperationExpression ParseBinaryOperationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var left = ParsePrefixUnaryOperationExpression(tokens, ref position);

		if (TokenAt(tokens, position) is not { } op || GetBinaryOperation(op.Type) is not { } operation)
			return left;

		position++;

		if (left is not PrimaryOperationExpression primaryLeft)
		{
			throw new ParseException("Cannot define operator overload with multiple operators", left.op!, left.range);
		}

		var right = new PrimaryOperationExpression(ParseParameter(tokens, ref position, false));
		return new BinaryOperationExpression(primaryLeft.operand, operation, op, right.operand,
			left.range.Join(right.range));
	}

	private OperationExpression ParsePrefixUnaryOperationExpression(IReadOnlyList<Token> tokens,
		ref int position)
	{
		if (TokenAt(tokens, position) is not { } op || GetPrefixUnaryOperation(op.Type) is not { } operation)
			return ParsePostfixUnaryOperationExpression(tokens, ref position);

		if (operation == UnaryExpression.Operation.Await)
			throw new ParseException("Cannot overload 'await' operator", op);
		
		position++;
		var operand = ParseParameter(tokens, ref position, false);
		return new UnaryOperationExpression(operation, op, operand, true, op.Range.Join(operand.range));
	}

	private OperationExpression ParsePostfixUnaryOperationExpression(IReadOnlyList<Token> tokens,
		ref int position)
	{
		var operand = ParseParameter(tokens, ref position, false);
		
		if (TokenAt(tokens, position) is not { } op || GetPostfixUnaryOperation(op.Type) is not { } operation)
			return new PrimaryOperationExpression(operand);
		
		position++;
		return new UnaryOperationExpression(operation, op, operand, false, operand.range.Join(op.Range));
	}

	private List<StatementNode> ParseTopLevelStatements(IReadOnlyList<Token> tokens, ref int position, TokenType? endTokenType = null)
	{
		var syncTokens = new[]
		{
			TokenType.EndOfFile,
			TokenType.KeywordModule,
			TokenType.KeywordEntry,
			TokenType.KeywordFun,
			TokenType.KeywordDef,
			TokenType.KeywordImplicit,
			TokenType.KeywordExplicit,
			TokenType.KeywordVar,
			TokenType.KeywordLet,
			TokenType.KeywordConst,
			TokenType.KeywordStruct,
			TokenType.KeywordInterface,
			TokenType.KeywordCast,
			TokenType.KeywordOperator,
			TokenType.OpRightBrace
		};
		
		var statements = new List<StatementNode>();

		while (Peek(tokens, position) != TokenType.EndOfFile && Peek(tokens, position) != endTokenType)
		{
			try
			{
				statements.Add(ParseTopLevelStatement(tokens, ref position));
			}
			catch (ParseException e)
			{
				currentDiagnostics.Add(e);

				do
				{
					position++;
				} while (!syncTokens.Contains(Peek(tokens, position)));
			}
		}
		
		return statements;
	}

	private StatementNode ParseTopLevelStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var peek = Peek(tokens, position);
		if (peek == TokenType.KeywordModule)
		{
			return ParseModuleStatement(tokens, ref position, true);
		}

		if (peek == TokenType.KeywordConst)
		{
			return ParseVarDeclaration(tokens, ref position);
		}
		
		if (TryParseEntryStatement(tokens, ref position, out var entryStatement))
		{
			return entryStatement;
		}
		
		if (TryParseDllImportStatement(tokens, ref position, out var dllImportStatement))
		{
			return dllImportStatement;
		}
		
		if (TryParseExternalFunctionStatement(tokens, ref position, out var externalFunctionStatement))
		{
			return externalFunctionStatement;
		}
		
		if (TryParseFunctionDeclaration(tokens, ref position, out var functionDeclaration))
		{
			return functionDeclaration;
		}
		
		if (TryParseStructDeclaration(tokens, ref position, out var structDeclaration))
		{
			return structDeclaration;
		}
		
		if (TryParseDefineStatement(tokens, ref position, out var defineStatement))
		{
			return defineStatement;
		}

		throw new ParseException($"Expected top-level statement; Instead, got '{peek}'", tokens[position]);
	}

	private List<ExternalFunctionStatement> ParseDllImportedStatements(IReadOnlyList<Token> tokens,
		ref int position, TokenType? endTokenType = null)
	{
		var syncTokens = new[]
		{
			TokenType.EndOfFile,
			TokenType.KeywordVar,
			TokenType.KeywordStatic,
			TokenType.KeywordFun
		};
		
		var statements = new List<ExternalFunctionStatement>();

		while (Peek(tokens, position) != TokenType.EndOfFile && Peek(tokens, position) != endTokenType)
		{
			try
			{
				statements.Add(ParseDllImportedStatement(tokens, ref position));
			}
			catch (ParseException e)
			{
				currentDiagnostics.Add(e);

				do
				{
					position++;
				} while (!syncTokens.Contains(Peek(tokens, position)));
			}
		}
		
		return statements;
	}

	private ExternalFunctionStatement ParseDllImportedStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var peek = Peek(tokens, position);
		if (peek == TokenType.KeywordFun || peek == TokenType.KeywordVar)
		{
			return ParseExternalFunctionStatement(tokens, ref position);
		}

		throw new ParseException($"Expected imported statement; Instead, got '{peek}'", tokens[position]);
	}

	private BlockStatement ParseBlockStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftBrace);
		var statements = ParseStatements(tokens, ref position, TokenType.OpRightBrace);
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBrace);

		return new BlockStatement(statements, startToken.Range.Join(endToken.Range));
	}

	private List<StatementNode> ParseStatements(IReadOnlyList<Token> tokens, ref int position,
		TokenType endTokenType)
	{
		// Todo: Add all statement start tokens (loops, etc.)
		var syncTokens = new[]
		{
			TokenType.EndOfFile,
			TokenType.KeywordLet,
			TokenType.KeywordVar,
			TokenType.KeywordConst,
			TokenType.KeywordStatic,
			TokenType.KeywordFun,
			TokenType.KeywordReturn,
			TokenType.KeywordIf,
			TokenType.KeywordSwitch,
			TokenType.Identifier,
		};
		
		var statements = new List<StatementNode>();

		while (Peek(tokens, position) != TokenType.EndOfFile && Peek(tokens, position) != endTokenType)
		{
			try
			{
				statements.Add(ParseStatement(tokens, ref position));
			}
			catch (ParseException e)
			{
				currentDiagnostics.Add(e);

				do
				{
					position++;
				} while (!syncTokens.Contains(Peek(tokens, position)));
			}
		}
		
		return statements;
	}

	private StatementNode ParseStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var peek = Peek(tokens, position);
		
		if (peek == TokenType.KeywordVar || peek == TokenType.KeywordLet || peek == TokenType.KeywordConst)
		{
			return ParseVarDeclaration(tokens, ref position);
		}
			
		if (peek == TokenType.KeywordReturn)
		{
			return ParseReturnStatement(tokens, ref position);
		}
			
		if (peek == TokenType.KeywordIf)
		{
			return ParseIfStatement(tokens, ref position);
		}

		if (peek == TokenType.EndOfFile)
			throw tokens.LastOrDefault() is { } token
				? new ParseException("Expected statement", token)
				: new ParseException("Expected statement", source, TextRange.Empty);
		
		var expression = ParseExpression(tokens, ref position);
		var expressionStatement = new ExpressionStatement(expression, expression.range);
		return expressionStatement;
	}

	private VarDeclarationStatement ParseVarDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		if (Match(tokens, ref position, TokenType.KeywordVar))
		{
			// Todo: Support tuples of identifiers
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			var range = identifier.Range;
			
			SyntaxType? type = null;
			if (Match(tokens, ref position, TokenType.OpColon))
			{
				type = ParseType(tokens, ref position);
				range = range.Join(type.range);
			}

			ExpressionNode? initializer = null;
			if (Match(tokens, ref position, TokenType.OpEquals))
			{
				initializer = ParseExpression(tokens, ref position);
				range = range.Join(initializer.range);
			}

			return new MutableVarDeclarationStatement(identifier, type, initializer, range);
		}
		
		if (Match(tokens, ref position, TokenType.KeywordLet))
		{
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			
			SyntaxType? type = null;
			if (Match(tokens, ref position, TokenType.OpColon))
			{
				type = ParseType(tokens, ref position);
			}

			Consume(tokens, ref position, "Immutable variable declarations require an initial value",
				TokenType.OpEquals);
			var initializer = ParseExpression(tokens, ref position);

			return new ImmutableVarDeclarationStatement(identifier, type, initializer,
				identifier.Range.Join(initializer.range));
		}
		
		if (Match(tokens, ref position, TokenType.KeywordConst))
		{
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			
			SyntaxType? type = null;
			if (Match(tokens, ref position, TokenType.OpColon))
			{
				type = ParseType(tokens, ref position);
			}

			Consume(tokens, ref position, "Constant variable declarations require a value", TokenType.OpEquals);
			var initializer = ParseExpression(tokens, ref position);

			return new ConstVarDeclarationStatement(identifier, type, initializer,
				identifier.Range.Join(initializer.range));
		}

		if (IsEndOfFile(tokens, position))
			throw tokens.LastOrDefault() is { } token
				? new ParseException("Expected variable declaration", token)
				: new ParseException("Expected variable declaration", source, TextRange.Empty);
		
		throw new ParseException("Expected variable declaration", tokens[position]);
	}

	private ReturnStatement ParseReturnStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordReturn);
		var expression = ParseExpression(tokens, ref position);

		return new ReturnStatement(expression, startToken.Range.Join(expression.range));
	}

	private IfStatement ParseIfStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordIf);
		var usesParen = Match(tokens, ref position, TokenType.OpLeftParen);
		var condition = ParseExpression(tokens, ref position);

		if (usesParen)
			Consume(tokens, ref position, null, TokenType.OpRightParen);

		var thenBranch = ParseStatement(tokens, ref position);

		StatementNode? elseBranch = null;
		if (Match(tokens, ref position, TokenType.KeywordElse))
			elseBranch = ParseStatement(tokens, ref position);

		return new IfStatement(condition, thenBranch, elseBranch, startToken.Range.Join(condition.range));
	}

	private BinaryExpression.Operation? GetBinaryOperation(TokenType op)
	{
		if (op == TokenType.OpPlus)
			return BinaryExpression.Operation.Add;
		
		if (op == TokenType.OpMinus)
			return BinaryExpression.Operation.Subtract;
		
		if (op == TokenType.OpStarStar)
			return BinaryExpression.Operation.Power;
		
		if (op == TokenType.OpStar)
			return BinaryExpression.Operation.Multiply;
		
		if (op == TokenType.OpSlash)
			return BinaryExpression.Operation.Divide;
		
		if (op == TokenType.OpPlusPercent)
			return BinaryExpression.Operation.PosMod;
		
		if (op == TokenType.OpPercent)
			return BinaryExpression.Operation.Modulo;
		
		if (op == TokenType.OpRotRight)
			return BinaryExpression.Operation.RotRight;
		
		if (op == TokenType.OpRotLeft)
			return BinaryExpression.Operation.RotLeft;
		
		if (op == TokenType.OpRightRight)
			return BinaryExpression.Operation.ShiftRight;
		
		if (op == TokenType.OpLeftLeft)
			return BinaryExpression.Operation.ShiftLeft;
		
		if (op == TokenType.OpGreater)
			return BinaryExpression.Operation.GreaterThan;
		
		if (op == TokenType.OpLess)
			return BinaryExpression.Operation.LessThan;
		
		if (op == TokenType.OpGreaterEqual)
			return BinaryExpression.Operation.GreaterEqual;
		
		if (op == TokenType.OpLessEqual)
			return BinaryExpression.Operation.LessEqual;
		
		if (op == TokenType.OpEqualsEquals)
			return BinaryExpression.Operation.Equals;
		
		if (op == TokenType.OpBangEquals)
			return BinaryExpression.Operation.NotEquals;

		return null;
	}

	private ExpressionNode ParseExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TryParseLambdaExpression(tokens, ref position, out var lambdaExpression))
			return lambdaExpression;
		
		return ParseAssignmentExpression(tokens, ref position);
	}

	private ExpressionNode ParseAssignmentExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseConditionalExpression(tokens, ref position);

		while (Match(tokens, ref position, TokenType.OpEquals))
		{
			var valueExpression = ParseExpression(tokens, ref position);
			expression = new AssignmentExpression(expression, valueExpression,
				expression.range.Join(valueExpression.range));
		}
		
		return expression;
	}

	private ExpressionNode ParseConditionalExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseNullCoalescingExpression(tokens, ref position);

		if (!Match(tokens, ref position, TokenType.OpQuestion))
			return expression;
		
		var trueExpression = ParseExpression(tokens, ref position);
		var range = expression.range.Join(trueExpression.range);

		ExpressionNode? falseExpression = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			falseExpression = ParseExpression(tokens, ref position);
			range = range.Join(falseExpression.range);
		}

		return new ConditionalExpression(expression, trueExpression, falseExpression, range);
	}
	
	private ExpressionNode ParseNullCoalescingExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseEqualityExpression(tokens, ref position);

		while (Match(tokens, ref position, out var op, TokenType.OpQuestionQuestion))
		{
			var right = ParseExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.NullCoalescence, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseEqualityExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseOrExpression(tokens, ref position);
		
		while (Match(tokens, ref position, out var op, TokenType.OpEqualsEquals, TokenType.OpBangEquals))
		{
			var right = ParseOrExpression(tokens, ref position);
			var range = expression.range.Join(right.range);

			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpEqualsEquals)
				operation = BinaryExpression.Operation.Equals;
			else if (op.Type == TokenType.OpBangEquals)
				operation = BinaryExpression.Operation.NotEquals;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseOrExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseXorExpression(tokens, ref position);
		
		while (Match(tokens, ref position, out var op, TokenType.OpBar))
		{
			var right = ParseXorExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.Or, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseXorExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseAndExpression(tokens, ref position);
		
		while (Match(tokens, ref position, out var op, TokenType.OpHat))
		{
			var right = ParseAndExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.Xor, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseAndExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseRelationalExpression(tokens, ref position);
		
		while (Match(tokens, ref position, out var op, TokenType.OpAmp))
		{
			var right = ParseRelationalExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.And, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseRelationalExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseShiftExpression(tokens, ref position);

		if (Match(tokens, ref position, out var op, TokenType.OpLessEqual, TokenType.OpGreaterEqual, TokenType.OpLess,
			    TokenType.OpGreater)) 
		{
			var right = ParseShiftExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpLessEqual)
				operation = BinaryExpression.Operation.LessEqual;
			else if (op.Type == TokenType.OpGreaterEqual)
				operation = BinaryExpression.Operation.GreaterEqual;
			else if (op.Type == TokenType.OpLess)
				operation = BinaryExpression.Operation.LessThan;
			else if (op.Type == TokenType.OpGreater)
				operation = BinaryExpression.Operation.GreaterThan;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, op, right, range);
		}
		else if (Match(tokens, ref position, out var castOp, TokenType.KeywordIs, TokenType.KeywordAs)) 
		{
			var right = ParseType(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (castOp.Type == TokenType.KeywordIs)
				operation = BinaryExpression.Operation.Is;
			else if (castOp.Type == TokenType.KeywordAs)
				operation = BinaryExpression.Operation.As;
			else
				throw new ParseException("Unexpected operation", castOp);
			
			expression = new BinaryExpression(expression, operation, castOp, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseShiftExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseAdditiveExpression(tokens, ref position);

		while (Match(tokens, ref position, out var op, TokenType.OpRotLeft, TokenType.OpRotRight, TokenType.OpLeftLeft,
			       TokenType.OpRightRight))
		{
			var right = ParseAdditiveExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpRotLeft)
				operation = BinaryExpression.Operation.RotLeft;
			else if (op.Type == TokenType.OpRotRight)
				operation = BinaryExpression.Operation.RotRight;
			else if (op.Type == TokenType.OpLeftLeft)
				operation = BinaryExpression.Operation.ShiftLeft;
			else if (op.Type == TokenType.OpRightRight)
				operation = BinaryExpression.Operation.ShiftRight;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseAdditiveExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseMultiplicativeExpression(tokens, ref position);

		while (Match(tokens, ref position, out var op, TokenType.OpPlus, TokenType.OpMinus))
		{
			var right = ParseMultiplicativeExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpPlus)
				operation = BinaryExpression.Operation.Add;
			else if (op.Type == TokenType.OpMinus)
				operation = BinaryExpression.Operation.Subtract;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseMultiplicativeExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseExponentiationExpression(tokens, ref position);

		while (Match(tokens, ref position, out var op, TokenType.OpStar, TokenType.OpSlash, TokenType.OpPlusPercent,
			       TokenType.OpPercent)) 
		{
			var right = ParseExponentiationExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpStar)
				operation = BinaryExpression.Operation.Multiply;
			else if (op.Type == TokenType.OpSlash)
				operation = BinaryExpression.Operation.Divide;
			else if (op.Type == TokenType.OpPlusPercent)
				operation = BinaryExpression.Operation.PosMod;
			else if (op.Type == TokenType.OpPercent)
				operation = BinaryExpression.Operation.Modulo;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseExponentiationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseSwitchWithExpression(tokens, ref position);

		if (Match(tokens, ref position, out var op, TokenType.OpStarStar)) 
		{
			var right = ParseExponentiationExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.Power, op, right, range);
		}

		return expression;
	}

	private ExpressionNode ParseSwitchWithExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (Peek(tokens, position) == TokenType.KeywordSwitch)
		{
			return ParseSwitchExpression(tokens, ref position);
		}
		
		if (Peek(tokens, position) == TokenType.KeywordWith)
		{
			return ParseWithExpression(tokens, ref position);
		}
		
		return ParseRangeExpression(tokens, ref position);
	}

	private SwitchExpression ParseSwitchExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordSwitch);
		
		// Todo
		return new SwitchExpression(startToken.Range);
	}

	private WithExpression ParseWithExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordWith);
		
		// Todo
		return new WithExpression(startToken.Range);
	}

	private ExpressionNode ParseRangeExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParsePrefixUnaryExpression(tokens, ref position);

		while (Match(tokens, ref position, out var op, TokenType.OpDotDotEqual, TokenType.OpDotDot)) 
		{
			var right = ParsePrefixUnaryExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpDotDotEqual)
				operation = BinaryExpression.Operation.RangeInclusive;
			else if (op.Type == TokenType.OpDotDot)
				operation = BinaryExpression.Operation.RangeExclusive;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, op, right, range);
		}

		return expression;
	}

	private UnaryExpression.Operation? GetPrefixUnaryOperation(TokenType op)
	{
		if (op == TokenType.OpPlusPlus)
			return UnaryExpression.Operation.PreIncrement;
		
		if (op == TokenType.OpMinusMinus)
			return UnaryExpression.Operation.PreDecrement;
		
		if (op == TokenType.OpPlus)
			return UnaryExpression.Operation.Identity;
		
		if (op == TokenType.OpMinus)
			return UnaryExpression.Operation.Negate;
		
		if (op == TokenType.OpTilde)
			return UnaryExpression.Operation.BitwiseNegate;
		
		if (op == TokenType.OpBang)
			return UnaryExpression.Operation.LogicalNot;
		
		if (op == TokenType.KeywordAwait)
			return UnaryExpression.Operation.Await;

		return null;
	}

	private UnaryExpression.Operation? GetPostfixUnaryOperation(TokenType op)
	{
		if (op == TokenType.OpPlusPlus)
			return UnaryExpression.Operation.PostIncrement;
		
		if (op == TokenType.OpMinusMinus)
			return UnaryExpression.Operation.PostDecrement;

		return null;
	}

	private ExpressionNode ParsePrefixUnaryExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TokenAt(tokens, position) is not { } op || GetPrefixUnaryOperation(op.Type) is not { } operation)
			return ParsePostfixUnaryExpression(tokens, ref position);

		position++;
		var right = ParsePrefixUnaryExpression(tokens, ref position);
		var range = op.Range.Join(right.range);
		
		// Unary(Literal, Negate) -> Negated Literal
		if (right is TokenExpression tokenExpression)
		{
			var token = tokenExpression.token;
			if (token.Type == TokenType.NumberLiteral)
			{
				switch (operation)
				{
					case UnaryExpression.Operation.Identity:
						return tokenExpression;
					
					case UnaryExpression.Operation.Negate:
						switch (token.Value)
						{
							case sbyte value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									(sbyte)-value));	
							
							case short value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									(short)-value));
							
							case int value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									-value));
							
							case long value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									-value));	
						}
						break;
					
					case UnaryExpression.Operation.BitwiseNegate:
						switch (token.Value)
						{
							case sbyte value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									(sbyte)~value));
							
							case byte value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									(byte)~value));
							
							case short value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									(short)~value));
							
							case ushort value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									(ushort)~value));
							
							case int value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									~value));
							
							case uint value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									~value));
							
							case long value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									~value));
							
							case ulong value:
								return new TokenExpression(new Token(token.Type, token.Range, token.Source,
									~value));
						}
						break;
				}
			}
			else if (token.Type == TokenType.KeywordTrue || token.Type == TokenType.KeywordFalse)
			{
				if (operation == UnaryExpression.Operation.LogicalNot)
					return new TokenExpression(new Token(token.Type, token.Range, token.Source, !(bool)token.Value!));
			}
		}
			
		return new UnaryExpression(right, operation, op, true, range);
	}

	private bool TryParseLambdaExpression(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out LambdaExpression? lambdaExpression)
	{
		var startPosition = position;

		try
		{
			lambdaExpression = ParseLambdaExpression(tokens, ref position);
			return true;
		}
		catch (ParseException)
		{
			lambdaExpression = null;
			position = startPosition;
			return false;
		}
	}
	
	private LambdaExpression ParseLambdaExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var parameters = new List<Parameter>();
		var startPosition = position;
		
		if (Match(tokens, ref position, TokenType.OpLeftParen))
		{
			do
			{
				parameters.Add(ParseLambdaParameter(tokens, ref position));
			} while (Match(tokens, ref position, TokenType.OpComma));

			Consume(tokens, ref position, null, TokenType.OpRightParen);
		}
		else parameters.Add(ParseLambdaParameter(tokens, ref position));

		var startToken = tokens[startPosition];

		SyntaxType? returnType = null;
		if (Match(tokens, ref position, TokenType.OpReturnType))
		{
			returnType = ParseType(tokens, ref position);
		}

		StatementNode body;

		if (Match(tokens, ref position, TokenType.OpDoubleArrow))
		{
			var expression = ParseExpression(tokens, ref position);
			body = new ReturnStatement(expression, expression.range);
		}
		else
			body = ParseBlockStatement(tokens, ref position);

		return new LambdaExpression(parameters, returnType, body, startToken.Range.Join(body.range));
	}

	private Parameter ParseLambdaParameter(IReadOnlyList<Token> tokens, ref int position)
	{
		var isMutable = Match(tokens, ref position, TokenType.KeywordVar);
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		Consume(tokens, ref position, null, TokenType.OpColon);
		var type = ParseType(tokens, ref position);

		return new Parameter(identifier, type, null, false, isMutable, identifier.Range.Join(type.range));
	}

	private Parameter ParseParameter(IReadOnlyList<Token> tokens, ref int position, bool defaultValueAllowed)
	{
		var isVariadic = Match(tokens, ref position, TokenType.OpEllipsis);
		var isMutable = Match(tokens, ref position, TokenType.KeywordVar);
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		var range = identifier.Range;

		SyntaxType? type = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			type = ParseType(tokens, ref position);
			range = range.Join(type.range);
		}

		if (!defaultValueAllowed || !Match(tokens, ref position, TokenType.OpEquals))
			return new Parameter(identifier, type, null, isVariadic, isMutable, range);
		
		var defaultExpression = ParseExpression(tokens, ref position);
		range = range.Join(defaultExpression.range);

		return new Parameter(identifier, type, defaultExpression, isVariadic, isMutable, range);
	}

	private ExpressionNode ParsePostfixUnaryExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var start = position;
		var expression = ParsePrimaryExpression(tokens, ref position);

		var functionCallAllowed = true;
		while (true)
		{
			var peek = Peek(tokens, position);
			
			// Cast - Warning: x::single.abs() is parsed as (x::single).abs()
			if (peek == TokenType.OpColonColon)
			{
				expression = ParseCastExpression(tokens, ref position, expression);
				break;
			}
			
			// Function call
			if (peek == TokenType.OpLeftParen)
			{
				if (!functionCallAllowed)
					break;
				
				// Todo - handle function call chaining? not currently possible because of lack of semicolons
				expression = ParseFunctionCallExpression(tokens, ref position, expression);
				functionCallAllowed = false;
				continue;
			}
			
			// Access
			if (peek == TokenType.OpQuestionDot || peek == TokenType.OpDot)
			{
				expression = ParseAccessExpression(tokens, ref position, expression);
				functionCallAllowed = true;
				continue;
			}
			
			// Index
			if (peek == TokenType.OpQuestionLeftBracket || peek == TokenType.OpLeftBracket)
			{
				try
				{
					expression = ParseIndexExpression(tokens, ref position, expression);
				}
				catch
				{
					if (expression is TokenExpression)
					{
						position = start;
						expression = ParseType(tokens, ref position);
					}
					else throw;
				}

				functionCallAllowed = true;
				continue;
			}
			
			// Instantiation
			if (peek == TokenType.OpLeftBrace)
			{
				expression = ParseInstantiationExpression(tokens, ref position, expression);
				functionCallAllowed = true;
				continue;
			}
			
			// ++, --
			if (GetPostfixUnaryOperation(peek) is { } operation)
			{
				var op = TokenAt(tokens, position)!;
				var endToken = tokens[position++];
				expression = new UnaryExpression(expression, operation, op, false,
					expression.range.Join(endToken.Range));
				continue;
			}

			break;
		}

		return expression;
	}

	private FunctionCallExpression ParseFunctionCallExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode caller)
	{
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		var args = new List<ExpressionNode>();
		
		if (Peek(tokens, position) != TokenType.OpRightParen)
			args = ParseArgumentList(tokens, ref position);
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		return new FunctionCallExpression(caller, args, caller.range.Join(endToken.Range));
	}

	private CastExpression ParseCastExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode source)
	{
		Consume(tokens, ref position, null, TokenType.OpColonColon);
		var targetType = ParseType(tokens, ref position);

		return new CastExpression(source, targetType, source.range.Join(targetType.range));
	}

	private AccessExpression ParseAccessExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode source)
	{
		var accessOperator = Consume(tokens, ref position, null, TokenType.OpQuestionDot, TokenType.OpDot);
		var nullCheck = accessOperator.Type == TokenType.OpQuestionDot;
		var target = Consume(tokens, ref position, null, TokenType.Identifier, TokenType.KeywordNew,
			TokenType.KeywordString);

		return new AccessExpression(source, target, nullCheck, source.range.Join(target.Range));
	}

	private IndexExpression ParseIndexExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode source)
	{
		var accessOperator = Consume(tokens, ref position, null, TokenType.OpQuestionLeftBracket,
			TokenType.OpLeftBracket);
		var nullCheck = accessOperator.Type == TokenType.OpQuestionLeftBracket;
		var index = ParseExpression(tokens, ref position);
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBracket);

		return new IndexExpression(source, index, nullCheck, source.range.Join(endToken.Range));
	}

	private List<ExpressionNode> ParseArgumentList(IReadOnlyList<Token> tokens, ref int position)
	{
		var args = new List<ExpressionNode>();
		
		do
		{
			args.Add(ParseExpression(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));

		return args;
	}
	
	private ExpressionNode ParsePrimaryExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (Match(tokens, ref position, out var literal, TokenType.NumberLiteral, TokenType.StringLiteral,
			    TokenType.CharLiteral, TokenType.KeywordThis, TokenType.KeywordTrue, TokenType.KeywordFalse,
			    TokenType.KeywordNull, TokenType.KeywordNew))
		{
			return new TokenExpression(literal);
		}

		var peek = TokenAt(tokens, position);

		if (peek?.Type is { } peekType)
		{
			if (peekType == TokenType.InterpolatedStringLiteral)
				return ParseInterpolatedString(tokens, ref position);
			
			if (peekType == TokenType.OpLeftParen)
				return ParseTupleExpression(tokens, ref position);

			if (peekType == TokenType.OpLeftBracket)
				return ParseListExpression(tokens, ref position);

			if (peekType == TokenType.Identifier)
				return new TokenExpression(Consume(tokens, ref position, null, TokenType.Identifier));

			if (peekType == TokenType.KeywordRef || TokenType.NativeDataTypes.Contains(peekType))
				return ParseType(tokens, ref position);
			
			throw new ParseException($"Expected expression; Instead, got '{peekType}'", peek);
		}

		if (peek is not null)
			throw new ParseException($"Expected expression; Instead, got '{peek.Type?.ToString() ?? "null"}'", peek);
		
		throw new ParseException("Expected expression; Instead, got end of file", source, TextRange.Empty);
	}

	private readonly struct InterpolationPart
	{
		public readonly string text;
		public readonly bool isStringLiteral;
		public readonly TextRange range;

		public InterpolationPart(string text, bool isStringLiteral, TextRange range)
		{
			this.text = text;
			this.isStringLiteral = isStringLiteral;
			this.range = range;
		}
	}

	private InterpolatedStringExpression ParseInterpolatedString(IReadOnlyList<Token> tokens, ref int position)
	{
		var stringLiteral = Consume(tokens, ref position, null, TokenType.InterpolatedStringLiteral);
		if (stringLiteral.Value is not string text)
			throw new ParseException("Malformed interpolated string", stringLiteral);
		
		var stringParts = new List<InterpolationPart>();

		var escaped = false;
		var withinString = true;
		var rangeStart = stringLiteral.Range.Start + 1;
		var start = 0;
		for (var i = 0; i < text.Length; i++)
		{
			var character = text[i];

			if (!withinString)
			{
				switch (character)
				{
					case '"':
						withinString = true;
						start = i + 1;
						break;
					case '}':
						withinString = true;
						
						var partText = text[start..i];
						if (partText != "")
							stringParts.Add(
								new InterpolationPart(partText, false, new TextRange(start, i) + rangeStart));
						
						start = i + 1;
						break;
				}

				continue;
			}
			
			if (character == '\\')
			{
				escaped = !escaped;
				continue;
			}

			if (character == '{' && !escaped)
			{
				var partText = text[start..i];
				if (partText != "")
					stringParts.Add(new InterpolationPart(partText, true, new TextRange(start, i) + rangeStart));
				
				start = i + 1;
				withinString = false;
			}

			if (character == '"' && !escaped)
			{
				var partText = text[start..i];
				if (partText != "")
					stringParts.Add(new InterpolationPart(partText, true, new TextRange(start, i) + rangeStart));
				
				start = i + 1;
				withinString = false;
			}

			escaped = false;
		}

		if (withinString)
		{
			var partText = text[start..];
			if (partText != "")
				stringParts.Add(new InterpolationPart(partText, true, new TextRange(start, text.Length) + rangeStart));
		}

		var parts = new List<ExpressionNode>();
		foreach (var part in stringParts)
		{
			if (part.isStringLiteral)
			{
				var value = Lexer.UnescapeString(part.text, true);
				var token = new Token(TokenType.StringLiteral, part.range, stringLiteral.Source, value);
				parts.Add(new TokenExpression(token));
			}
			else
			{
				var partSource = new StringBuffer(part.text);
				var lexer = new FilteredLexer(partSource);
				var interpolatedPosition = 0;
				var interpolatedTokens = new List<Token>();

				foreach (var interpolatedToken in lexer)
				{
					if (interpolatedToken.Type.IsInvalid)
					{
						var plural = interpolatedToken.Text.Length > 1 ? "characters" : "character";
						throw new ParseException($"Unexpected {plural} in interpolated string", interpolatedToken);
					}

					interpolatedTokens.Add(new Token(interpolatedToken.Type, interpolatedToken.Range + part.range.Start,
						stringLiteral.Source, interpolatedToken.Value));
				}
				
				parts.Add(ParseExpression(interpolatedTokens, ref interpolatedPosition));
			}
		}

		return new InterpolatedStringExpression(parts, stringLiteral.Range);
	}

	private ExpressionNode ParseTupleExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var expressions = new List<ExpressionNode>();
		do
		{
			var expression = ParseExpression(tokens, ref position);
			expression.range = startToken.Range.Join(expression.range);
			expressions.Add(expression);
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		// If the tuple has only 1 value, it is actually a parenthesized expression, not a tuple
		if (expressions.Count == 1)
			return expressions[0];
		
		return new TupleExpression(expressions, startToken.Range.Join(endToken.Range));
	}

	private ExpressionNode ParseListExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TryParseMapExpression(tokens, ref position, out var mapExpression))
			return mapExpression;
		
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftBracket);

		var expressions = new List<ExpressionNode>();
		do
		{
			if (Peek(tokens, position) == TokenType.OpRightBracket)
				break;
			
			expressions.Add(ParseExpression(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBracket);
		var range = startToken.Range.Join(endToken.Range);
		
		SyntaxType? type = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			type = ParseType(tokens, ref position);
			range = range.Join(type.range);
		}

		return new ListExpression(expressions, type, range);
	}

	private bool TryParseMapExpression(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out MapExpression? mapExpression)
	{
        var startPosition = position;
		try
		{
			mapExpression = ParseMapExpression(tokens, ref position);
			return true;
		}
		catch (ParseException)
		{
			position = startPosition;
			mapExpression = null;
			return false;
		}
	}

	private MapExpression ParseMapExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftBracket);

		var expressions = new List<KeyValuePair<ExpressionNode, ExpressionNode>>();
		do
		{
			if (Peek(tokens, position) == TokenType.OpRightBracket)
				break;

			var keyExpression = ParseExpression(tokens, ref position);
			Consume(tokens, ref position, null, TokenType.OpDoubleArrow);
			var valueExpression = ParseExpression(tokens, ref position);
			
			expressions.Add(new KeyValuePair<ExpressionNode, ExpressionNode>(keyExpression, valueExpression));
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBracket);
		var range = startToken.Range.Join(endToken.Range);
		
		TupleSyntaxType? tupleType = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			var type = ParseTupleType(tokens, ref position);
			tupleType = type as TupleSyntaxType;

			if (tupleType?.types.Length != 2)
				throw new ParseException("Map type must be a tuple of two types", tokens[position - 1], type.range);
			
			range = range.Join(type.range);
		}

		return new MapExpression(expressions, tupleType, range);
	}

	private InstantiationExpression ParseInstantiationExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode typeExpression)
	{
		if (SyntaxType.TryConvert(typeExpression) is not { } type)
			throw new ParseException("Invalid instantiation type", source, typeExpression.range);
		
		Consume(tokens, ref position, null, TokenType.OpLeftBrace);

		var values = new Dictionary<Token, ExpressionNode>();
		do
		{
			if (Peek(tokens, position) == TokenType.OpRightBrace)
				break;
			
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			Consume(tokens, ref position, null, TokenType.OpEquals);
			var expression = ParseExpression(tokens, ref position);
			
			values.Add(identifier, expression);
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBrace);
		
		return new InstantiationExpression(type, values, type.range.Join(endToken.Range));
	}

	private SyntaxType ParseType(IReadOnlyList<Token> tokens, ref int position)
	{
		var peek = Peek(tokens, position);
		
		if (peek == TokenType.OpLeftParen)
			return ParseTupleType(tokens, ref position);

		if (peek == TokenType.KeywordFun)
			return ParseLambdaType(tokens, ref position);

		if (Match(tokens, ref position, out var mutableToken, TokenType.KeywordVar))
		{
			if (Match(tokens, ref position, TokenType.KeywordRef))
			{
				var mutRefType = ParseType(tokens, ref position);
				return new ReferenceSyntaxType(mutRefType, false, mutableToken.Range.Join(mutRefType.range));
			}
			
			var mutType = ParseType(tokens, ref position);
			return new MutableSyntaxType(mutType, mutableToken.Range.Join(mutType.range));
		}
		
		if (Match(tokens, ref position, out var refToken, TokenType.KeywordRef))
		{
			var refType = ParseType(tokens, ref position);
			return new ReferenceSyntaxType(refType, true, refToken.Range.Join(refType.range));
		}
		
		// Recursive types
		SyntaxType syntaxType = ParseBaseType(tokens, ref position);
		while (true)
		{
			// type[type1, type2, ...] or type[]
			if (Match(tokens, ref position, TokenType.OpLeftBracket))
			{
				if (Match(tokens, ref position, out var arrayRightBracket, TokenType.OpRightBracket))
				{
					syntaxType = new ArraySyntaxType(syntaxType, null, syntaxType.range.Join(arrayRightBracket.Range));
					continue;
				}

				try
				{
					var typeParameters = ParseTypeList(tokens, ref position);
					var genericRightBracket = Consume(tokens, ref position, null, TokenType.OpRightBracket);
					syntaxType = new GenericSyntaxType(syntaxType, typeParameters,
						syntaxType.range.Join(genericRightBracket.Range));
					continue;
				}
				catch (ParseException)
				{
					// Try again as type[expression] for array allocation
					var expression = ParseExpression(tokens, ref position);
					var rightBracket = Consume(tokens, ref position, null, TokenType.OpRightBracket);
					syntaxType = new ArraySyntaxType(syntaxType, expression, syntaxType.range.Join(rightBracket.Range));
					continue;
				}
			}

			if (Match(tokens, ref position, out var question, TokenType.OpQuestion))
			{
				syntaxType = new NullableSyntaxType(syntaxType, syntaxType.range.Join(question.Range));
				continue;
			}
			
			// Todo: Possibly support nested types
			
			break;
		}

		return syntaxType;
	}

	private BaseSyntaxType ParseBaseType(IReadOnlyList<Token> tokens, ref int position)
	{
		var token = Consume(tokens, ref position, "Invalid token for type", TokenType.ValidDataTypes.ToArray());
		return new BaseSyntaxType(token);
	}

	private LambdaSyntaxType ParseLambdaType(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordFun);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var parameterTypes = new List<SyntaxType>();
		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			parameterTypes.AddRange(ParseTypeList(tokens, ref position));
		}

		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		SyntaxType? returnType = null;
		if (Match(tokens, ref position, TokenType.OpReturnType))
			returnType = ParseType(tokens, ref position);

		var range = startToken.Range.Join(endToken.Range);
		if (returnType is not null)
			range = range.Join(returnType.range);

		return new LambdaSyntaxType(parameterTypes, returnType, range);
	}

	private List<SyntaxType> ParseTypeList(IReadOnlyList<Token> tokens, ref int position)
	{
		var types = new List<SyntaxType>();
		
		do
		{
			types.Add(ParseType(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));

		return types;
	}

	private SyntaxType ParseTupleType(IReadOnlyList<Token> tokens, ref int position)
	{
		var types = new List<SyntaxType>();
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftParen);

		do
		{
			types.Add(ParseType(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		// If the tuple has only 1 value, it is actually a parenthesized type, not a tuple
		if (types.Count == 1)
			return types[0];

		return new TupleSyntaxType(types, startToken.Range.Join(endToken.Range));
	}
}