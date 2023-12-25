using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public class ParseException : Exception
{
	public int Line { get; }
	public int Column { get; }

	public ParseException(string? message, Token? token, TextRange? range = null)
	: base(FormatMessage(message, token, range))
	{
		Line = token?.Line ?? 1;
		Column = token?.Column ?? 1;
	}

	private static string? FormatMessage(string? message, Token? token, TextRange? range)
	{
		if (message is null)
			return null;

		if (token is null)
			return message;

		var text = token.Text;
		if (range is not null)
			text = token.Source.GetText(range.Value);
		
		return $"[line {token.Line}, column {token.Column} at '{text}'] {message}";
	}
}

public static class Parser
{
	public static Ast? Parse(ILexer lexer, out List<ParseException> diagnostics)
	{
		var tokens = new List<Token>();

		diagnostics = [];
		foreach (var token in lexer)
		{
			tokens.Add(token);

			if (!token.Type.IsInvalid)
				continue;
			
			var plural = token.Text.Length > 1 ? "characters" : "character";
			diagnostics.Add(new ParseException($"Unexpected {plural}", token));
		}

		var root = ParseProgram(tokens, 0, diagnostics);
		diagnostics = diagnostics.OrderBy(d => d.Line).ThenBy(d => d.Column).ToList();
		
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

	private static Token Consume(IReadOnlyList<Token> tokens, ref int position, string? message,
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
			throw new ParseException($"{message}; Instead, got 'end of file'", tokens.LastOrDefault());

		var token = tokens[position];
		if (!types.Contains(token.Type))
			throw new ParseException($"{message}; Instead, got '{token.Type}'", token);
		
		position++;
		return token;
	}

	private static ProgramStatement? ParseProgram(IReadOnlyList<Token> tokens, int position,
		List<ParseException> diagnostics)
	{
		try
		{
			var startToken = TokenAt(tokens, position);
			var imports = ParseImportStatements(tokens, ref position);

			ModuleStatement? module = null;
			if (Peek(tokens, position) == TokenType.KeywordModule)
				module = ParseModuleStatement(tokens, ref position, false, diagnostics);
			
			var statements = ParseTopLevelStatements(tokens, ref position, diagnostics);

			if (Peek(tokens, position) != TokenType.EndOfFile)
				diagnostics.Add(new ParseException("Expected 'end of file'", TokenAt(tokens, position)));

			if (diagnostics.Count > 0)
				return null;

			var range = startToken is null
				? new TextRange(0, 0)
				: new TextRange(0, startToken.Source.Length - 1);

			return new ProgramStatement(imports, module, statements, range);
		}
		catch (ParseException e)
		{
			diagnostics.Add(e);
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
		
		if (!Match(tokens, ref position, out var startToken, TokenType.KeywordImport))
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

		var range = startToken.Range.Join(importToken.Range);

		Token? alias = null;
		if (importToken.Type == TokenType.Identifier)
		{
			if (Match(tokens, ref position, TokenType.KeywordAs))
			{
				alias = Consume(tokens, ref position, null, TokenType.Identifier);
				range = range.Join(alias.Range);
			}
		}

		import = new ImportStatement(scope, importToken, alias, range);
		return true;
	}

	private static ModuleStatement ParseModuleStatement(IReadOnlyList<Token> tokens, ref int position, bool requireBody,
		List<ParseException> diagnostics)
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
				diagnostics.Add(new ParseException("Invalid module statement: Expected 'identifier'", token));
		}

		var statements = new List<StatementNode>();
		if (Match(tokens, ref position, TokenType.OpLeftBrace))
		{
			statements.AddRange(ParseTopLevelStatements(tokens, ref position, diagnostics));
			var endToken = Consume(tokens, ref position, "Expected statement", TokenType.OpRightBrace);
			range = range.Join(endToken.Range);
		}
		else if (requireBody)
			diagnostics.Add(new ParseException("Module statement must have a body", TokenAt(tokens, position - 1)));

		return new ModuleStatement(scope, statements, range);
	}

	private static bool TryParseEntryStatement(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out EntryStatement? entry)
	{
		entry = null;
		if (Peek(tokens, position) != TokenType.KeywordEntry)
			return false;

		entry = ParseEntryStatement(tokens, ref position);
		return true;
	}

	private static EntryStatement ParseEntryStatement(IReadOnlyList<Token> tokens, ref int position)
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

	private static bool TryParseFunctionDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out FunctionDeclarationStatement? functionDeclaration)
	{
		functionDeclaration = null;
		if (Peek(tokens, position) != TokenType.KeywordFun)
			return false;

		functionDeclaration = ParseFunctionDeclaration(tokens, ref position);
		return true;
	}

	private static FunctionDeclarationStatement ParseFunctionDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordFun);
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);

		var typeParameters = new List<Type>();
		if (Match(tokens, ref position, TokenType.OpLeftBracket))
		{
			typeParameters.AddRange(ParseTypeList(tokens, ref position));
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
		
		Consume(tokens, ref position, null, TokenType.OpRightParen);

		Type? returnType = null;
		if (Match(tokens, ref position, TokenType.OpReturnType))
			returnType = ParseType(tokens, ref position);

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

		return new FunctionDeclarationStatement(identifier, typeParameters, parameters, returnType, body,
			startToken.Range.Join(body.range));
	}

	private static bool TryParseCastDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out CastDeclarationStatement? castDeclaration)
	{
		var validEntryTokens = new[] { TokenType.KeywordImplicit, TokenType.KeywordExplicit };
		
		castDeclaration = null;
		if (!validEntryTokens.Contains(Peek(tokens, position)))
			return false;

		castDeclaration = ParseCastDeclaration(tokens, ref position);
		return true;
	}

	private static CastDeclarationStatement ParseCastDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var castTypeToken = Consume(tokens, ref position, null, TokenType.KeywordImplicit, TokenType.KeywordExplicit);
		Consume(tokens, ref position, null, TokenType.KeywordCast);
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

		return new CastDeclarationStatement(castTypeToken.Type == TokenType.KeywordImplicit, parameter, returnType,
			body, castTypeToken.Range.Join(body.range));
	}

	private static bool TryParseOperatorDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out OperatorDeclarationStatement? operatorDeclaration)
	{
		operatorDeclaration = null;
		if (Peek(tokens, position) != TokenType.KeywordOperator)
			return false;

		operatorDeclaration = ParseOperatorDeclaration(tokens, ref position);
		return true;
	}

	private static OperatorDeclarationStatement ParseOperatorDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordOperator);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		var operationExpression = ParseOperationExpression(tokens, ref position);
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

		return new OperatorDeclarationStatement(operationExpression, returnType, body,
			startToken.Range.Join(body.range));
	}

	private static OperationExpression ParseOperationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		return ParseBinaryOperationExpression(tokens, ref position);
	}
	
	private static OperationExpression ParseBinaryOperationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var left = ParsePrefixUnaryOperationExpression(tokens, ref position);

		if (TokenAt(tokens, position) is not { } op || GetBinaryOperation(op.Type) is not { } operation)
			return left;

		position++;

		if (left is not PrimaryOperationExpression primaryLeft)
		{
			throw new ParseException("Cannot define operator overload with multiple operators", left.op, left.range);
		}

		var right = new PrimaryOperationExpression(ParseParameter(tokens, ref position, false));
		return new BinaryOperationExpression(primaryLeft.operand, operation, op, right.operand,
			left.range.Join(right.range));
	}

	private static OperationExpression ParsePrefixUnaryOperationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TokenAt(tokens, position) is not { } op || GetPrefixUnaryOperation(op.Type) is not { } operation)
			return ParsePostfixUnaryOperationExpression(tokens, ref position);

		if (operation == UnaryExpression.Operation.Await)
			throw new ParseException("Cannot overload 'await' operator", op);
		
		position++;
		var operand = ParseParameter(tokens, ref position, false);
		return new UnaryOperationExpression(operation, op, operand, true, op.Range.Join(operand.range));
	}

	private static OperationExpression ParsePostfixUnaryOperationExpression(IReadOnlyList<Token> tokens,
		ref int position)
	{
		var operand = ParseParameter(tokens, ref position, false);
		
		if (TokenAt(tokens, position) is not { } op || GetPostfixUnaryOperation(op.Type) is not { } operation)
			return new PrimaryOperationExpression(operand);
		
		position++;
		return new UnaryOperationExpression(operation, op, operand, false, operand.range.Join(op.Range));
	}

	private static List<StatementNode> ParseTopLevelStatements(IReadOnlyList<Token> tokens, ref int position,
		List<ParseException> diagnostics)
	{
		var syncTokens = new[]
		{
			TokenType.EndOfFile,
			TokenType.KeywordModule,
			TokenType.KeywordEntry,
			TokenType.KeywordFun,
			TokenType.KeywordImplicit,
			TokenType.KeywordExplicit,
			//TokenType.KeywordMutable,
			//TokenType.KeywordStruct,
			//TokenType.KeywordInterface
		};
		
		var statements = new List<StatementNode>();

		while (Peek(tokens, position) != TokenType.EndOfFile)
		{
			try
			{
				statements.Add(ParseTopLevelStatement(tokens, ref position, diagnostics));
			}
			catch (ParseException e)
			{
				diagnostics.Add(e);
				while (!syncTokens.Contains(Peek(tokens, position)))
					position++;
			}
		}
		
		return statements;
	}

	private static StatementNode ParseTopLevelStatement(IReadOnlyList<Token> tokens, ref int position,
		List<ParseException> diagnostics)
	{
		var peek = Peek(tokens, position);
		if (peek == TokenType.KeywordModule)
		{
			return ParseModuleStatement(tokens, ref position, true, diagnostics);
		}
			
		if (TryParseEntryStatement(tokens, ref position, out var entryStatement))
		{
			return entryStatement;
		}
			
		if (TryParseFunctionDeclaration(tokens, ref position, out var functionDeclaration))
		{
			return functionDeclaration;
		}
			
		if (TryParseCastDeclaration(tokens, ref position, out var castDeclaration))
		{
			return castDeclaration;
		}
			
		if (TryParseOperatorDeclaration(tokens, ref position, out var operationDeclaration))
		{
			return operationDeclaration;
		}

		throw new ParseException($"Expected top-level statement; Instead, got '{peek}'", tokens[position]);
	}

	private static BlockStatement ParseBlockStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftBrace);
		var statements = ParseStatements(tokens, ref position, TokenType.OpRightBrace);
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBrace);

		return new BlockStatement(statements, startToken.Range.Join(endToken.Range));
	}

	private static List<StatementNode> ParseStatements(IReadOnlyList<Token> tokens, ref int position,
		TokenType endTokenType)
	{
		var statements = new List<StatementNode>();

		while (Peek(tokens, position) != endTokenType)
		{
			statements.Add(ParseStatement(tokens, ref position));
		}
		
		return statements;
	}

	private static StatementNode ParseStatement(IReadOnlyList<Token> tokens, ref int position)
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
			throw new ParseException("Expected statement", tokens.LastOrDefault());
		
		var expression = ParseExpression(tokens, ref position);
		var expressionStatement = new ExpressionStatement(expression, expression.range);
		return expressionStatement;

	}

	private static VarDeclarationStatement ParseVarDeclaration(IReadOnlyList<Token> tokens, ref int position)
	{
		if (Match(tokens, ref position, TokenType.KeywordVar))
		{
			// Todo: Support tuples of identifiers
			var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
			var range = identifier.Range;
			
			Type? type = null;
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
			
			Type? type = null;
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
			
			Type? type = null;
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
			throw new ParseException("Expected variable declaration", tokens.LastOrDefault());
		
		throw new ParseException("Expected variable declaration", tokens[position]);
	}

	private static ReturnStatement ParseReturnStatement(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordReturn);
		var expression = ParseExpression(tokens, ref position);

		return new ReturnStatement(expression, startToken.Range.Join(expression.range));
	}

	private static IfStatement ParseIfStatement(IReadOnlyList<Token> tokens, ref int position)
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

	private static BinaryExpression.Operation? GetBinaryOperation(TokenType op)
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
		
		if (op == TokenType.OpPercentPercent)
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

	private static ExpressionNode ParseExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TryParseLambdaExpression(tokens, ref position, out var lambdaExpression))
			return lambdaExpression;
		
		return ParseAssignmentExpression(tokens, ref position);
	}

	private static ExpressionNode ParseAssignmentExpression(IReadOnlyList<Token> tokens, ref int position)
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

	private static ExpressionNode ParseConditionalExpression(IReadOnlyList<Token> tokens, ref int position)
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
	
	private static ExpressionNode ParseNullCoalescingExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseLogicalOrExpression(tokens, ref position);

		while (Match(tokens, ref position, TokenType.OpQuestionQuestion))
		{
			var right = ParseExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.NullCoalescence, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseLogicalOrExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseLogicalXorExpression(tokens, ref position);
		
		while (Match(tokens, ref position, TokenType.OpBarBar))
		{
			var right = ParseLogicalXorExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.LogicalOr, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseLogicalXorExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseLogicalAndExpression(tokens, ref position);
		
		while (Match(tokens, ref position, TokenType.OpHatHat))
		{
			var right = ParseLogicalAndExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.LogicalXor, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseLogicalAndExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseEqualityExpression(tokens, ref position);
		
		while (Match(tokens, ref position, TokenType.OpAmpAmp))
		{
			var right = ParseEqualityExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.LogicalAnd, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseEqualityExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseBitwiseOrExpression(tokens, ref position);
		
		while (Match(tokens, ref position, out var op, TokenType.OpEqualsEquals, TokenType.OpBangEquals))
		{
			var right = ParseBitwiseOrExpression(tokens, ref position);
			var range = expression.range.Join(right.range);

			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpEqualsEquals)
				operation = BinaryExpression.Operation.Equals;
			else if (op.Type == TokenType.OpBangEquals)
				operation = BinaryExpression.Operation.NotEquals;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseBitwiseOrExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseBitwiseXorExpression(tokens, ref position);
		
		while (Match(tokens, ref position, TokenType.OpBar))
		{
			var right = ParseBitwiseXorExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.BitwiseOr, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseBitwiseXorExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseBitwiseAndExpression(tokens, ref position);
		
		while (Match(tokens, ref position, TokenType.OpHat))
		{
			var right = ParseBitwiseAndExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.BitwiseXor, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseBitwiseAndExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseRelationalExpression(tokens, ref position);
		
		while (Match(tokens, ref position, TokenType.OpAmp))
		{
			var right = ParseRelationalExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.BitwiseAnd, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseRelationalExpression(IReadOnlyList<Token> tokens, ref int position)
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
			
			expression = new BinaryExpression(expression, operation, right, range);
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
			
			expression = new BinaryExpression(expression, operation, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseShiftExpression(IReadOnlyList<Token> tokens, ref int position)
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
			
			expression = new BinaryExpression(expression, operation, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseAdditiveExpression(IReadOnlyList<Token> tokens, ref int position)
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
			
			expression = new BinaryExpression(expression, operation, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseMultiplicativeExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseExponentiationExpression(tokens, ref position);

		while (Match(tokens, ref position, out var op, TokenType.OpStar, TokenType.OpSlash, TokenType.OpPercentPercent,
			       TokenType.OpPercent)) 
		{
			var right = ParseExponentiationExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			
			BinaryExpression.Operation operation;
			if (op.Type == TokenType.OpStar)
				operation = BinaryExpression.Operation.Multiply;
			else if (op.Type == TokenType.OpSlash)
				operation = BinaryExpression.Operation.Divide;
			else if (op.Type == TokenType.OpPercentPercent)
				operation = BinaryExpression.Operation.PosMod;
			else if (op.Type == TokenType.OpPercent)
				operation = BinaryExpression.Operation.Modulo;
			else
				throw new ParseException("Unexpected operation", op);
			
			expression = new BinaryExpression(expression, operation, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseExponentiationExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var expression = ParseSwitchWithExpression(tokens, ref position);

		if (Match(tokens, ref position, TokenType.OpStarStar)) 
		{
			var right = ParseExponentiationExpression(tokens, ref position);
			var range = expression.range.Join(right.range);
			expression = new BinaryExpression(expression, BinaryExpression.Operation.Power, right, range);
		}

		return expression;
	}

	private static ExpressionNode ParseSwitchWithExpression(IReadOnlyList<Token> tokens, ref int position)
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

	private static SwitchExpression ParseSwitchExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordSwitch);
		
		// Todo
		return new SwitchExpression(startToken.Range);
	}

	private static WithExpression ParseWithExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordWith);
		
		// Todo
		return new WithExpression(startToken.Range);
	}

	private static ExpressionNode ParseRangeExpression(IReadOnlyList<Token> tokens, ref int position)
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
			
			expression = new BinaryExpression(expression, operation, right, range);
		}

		return expression;
	}

	private static UnaryExpression.Operation? GetPrefixUnaryOperation(TokenType op)
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

	private static UnaryExpression.Operation? GetPostfixUnaryOperation(TokenType op)
	{
		if (op == TokenType.OpPlusPlus)
			return UnaryExpression.Operation.PostIncrement;
		
		if (op == TokenType.OpMinusMinus)
			return UnaryExpression.Operation.PostDecrement;

		return null;
	}

	private static ExpressionNode ParsePrefixUnaryExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (TokenAt(tokens, position) is not { } op || GetPrefixUnaryOperation(op.Type) is not { } operation)
			return ParsePostfixUnaryExpression(tokens, ref position);

		position++;
		var right = ParsePrefixUnaryExpression(tokens, ref position);
		var range = op.Range.Join(right.range);
			
		return new UnaryExpression(right, operation, true, range);
	}

	private static bool TryParseLambdaExpression(IReadOnlyList<Token> tokens, ref int position,
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
	
	private static LambdaExpression ParseLambdaExpression(IReadOnlyList<Token> tokens, ref int position)
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

		Type? returnType = null;
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

	private static Parameter ParseLambdaParameter(IReadOnlyList<Token> tokens, ref int position)
	{
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		Consume(tokens, ref position, null, TokenType.OpColon);
		var type = ParseType(tokens, ref position);

		return new Parameter(identifier, type, null, identifier.Range.Join(type.range));
	}

	private static Parameter ParseParameter(IReadOnlyList<Token> tokens, ref int position, bool defaultValueAllowed)
	{
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		var range = identifier.Range;

		Type? type = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			type = ParseType(tokens, ref position);
			range = range.Join(type.range);
		}

		if (!defaultValueAllowed || !Match(tokens, ref position, TokenType.OpEquals))
			return new Parameter(identifier, type, null, range);
		
		var defaultExpression = ParseExpression(tokens, ref position);
		range = range.Join(defaultExpression.range);

		return new Parameter(identifier, type, defaultExpression, range);
	}

	private static ExpressionNode ParsePostfixUnaryExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var start = position;
		var expression = ParsePrimaryExpression(tokens, ref position);

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
				expression = ParseFunctionCallExpression(tokens, ref position, expression);
				continue;
			}
			
			// Access
			if (peek == TokenType.OpQuestionDot || peek == TokenType.OpDot)
			{
				expression = ParseAccessExpression(tokens, ref position, expression);
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

				continue;
			}
			
			// Instantiation
			if (peek == TokenType.OpLeftBrace)
			{
				expression = ParseInstantiationExpression(tokens, ref position, expression);
				continue;
			}
			
			// ++, --
			if (GetPostfixUnaryOperation(peek) is { } operation)
			{
				var endToken = tokens[position++];
				expression = new UnaryExpression(expression, operation, false, expression.range.Join(endToken.Range));
				continue;
			}

			break;
		}

		return expression;
	}

	private static FunctionCallExpression ParseFunctionCallExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode caller)
	{
		Consume(tokens, ref position, null, TokenType.OpLeftParen);
		var args = new List<ExpressionNode>();
		
		if (Peek(tokens, position) != TokenType.OpRightParen)
			args = ParseArgumentList(tokens, ref position);
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		return new FunctionCallExpression(caller, args, caller.range.Join(endToken.Range));
	}

	private static CastExpression ParseCastExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode source)
	{
		Consume(tokens, ref position, null, TokenType.OpColonColon);
		var targetType = ParseType(tokens, ref position);

		return new CastExpression(source, targetType, source.range.Join(targetType.range));
	}

	private static AccessExpression ParseAccessExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode source)
	{
		var accessOperator = Consume(tokens, ref position, null, TokenType.OpQuestionDot, TokenType.OpDot);
		var nullCheck = accessOperator.Type == TokenType.OpQuestionDot;
		var target = ParsePrimaryExpression(tokens, ref position);

		return new AccessExpression(source, target, nullCheck, source.range.Join(target.range));
	}

	private static IndexExpression ParseIndexExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode source)
	{
		var accessOperator = Consume(tokens, ref position, null, TokenType.OpQuestionLeftBracket,
			TokenType.OpLeftBracket);
		var nullCheck = accessOperator.Type == TokenType.OpQuestionLeftBracket;
		var index = ParseExpression(tokens, ref position);
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBracket);

		return new IndexExpression(source, index, nullCheck, source.range.Join(endToken.Range));
	}

	private static List<ExpressionNode> ParseArgumentList(IReadOnlyList<Token> tokens, ref int position)
	{
		var args = new List<ExpressionNode>();
		
		do
		{
			args.Add(ParseExpression(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));

		return args;
	}
	
	private static ExpressionNode ParsePrimaryExpression(IReadOnlyList<Token> tokens, ref int position)
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
			if (peekType == TokenType.OpLeftParen)
			{
				return ParseTupleExpression(tokens, ref position);
			}

			if (peekType == TokenType.OpHashLeftBracket)
			{
				return ParseMapExpression(tokens, ref position);
			}

			if (peekType == TokenType.OpLeftBracket)
			{
				return ParseListExpression(tokens, ref position);
			}

			if (peekType == TokenType.Identifier)
			{
				return new TokenExpression(Consume(tokens, ref position, null, TokenType.Identifier));
			}

			if (TokenType.NativeDataTypes.Contains(peekType))
			{
				return ParseType(tokens, ref position);
			}
		}

		throw new ParseException("Expected expression", peek);
	}

	private static ExpressionNode ParseTupleExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var expressions = new List<ExpressionNode>();
		do
		{
			expressions.Add(ParseExpression(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		// If the tuple has only 1 value, it is actually a parenthesized expression, not a tuple
		if (expressions.Count == 1)
			return expressions[0];
		
		return new TupleExpression(expressions, startToken.Range.Join(endToken.Range));
	}

	private static ListExpression ParseListExpression(IReadOnlyList<Token> tokens, ref int position)
	{
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
		
		Type? type = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			type = ParseType(tokens, ref position);
			range = range.Join(type.range);
		}

		return new ListExpression(expressions, type, range);
	}

	private static MapExpression ParseMapExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.OpHashLeftBracket);

		var expressions = new List<(ExpressionNode, ExpressionNode)>();
		do
		{
			if (Peek(tokens, position) == TokenType.OpRightBracket)
				break;

			var keyExpression = ParseExpression(tokens, ref position);
			Consume(tokens, ref position, null, TokenType.OpDoubleArrow);
			var valueExpression = ParseExpression(tokens, ref position);
			
			expressions.Add((keyExpression, valueExpression));
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightBracket);
		var range = startToken.Range.Join(endToken.Range);
		
		TupleType? tupleType = null;
		if (Match(tokens, ref position, TokenType.OpColon))
		{
			var type = ParseTupleType(tokens, ref position);
			tupleType = type as TupleType;

			if (tupleType?.types.Length != 2)
				throw new ParseException("Map type must be a tuple of two types", tokens[position - 1], type.range);
			
			range = range.Join(type.range);
		}

		return new MapExpression(expressions, tupleType, range);
	}

	private static InstantiationExpression ParseInstantiationExpression(IReadOnlyList<Token> tokens, ref int position,
		ExpressionNode typeExpression)
	{
		if (Type.TryConvert(typeExpression) is not { } type)
			throw new ParseException("Invalid instantiation type", TokenAt(tokens, position), typeExpression.range);
		
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

	private static Type ParseType(IReadOnlyList<Token> tokens, ref int position)
	{
		var peek = Peek(tokens, position);
		
		if (peek == TokenType.OpLeftParen)
			return ParseTupleType(tokens, ref position);

		if (peek == TokenType.KeywordLambda)
			return ParseLambdaType(tokens, ref position);

		if (Match(tokens, ref position, out var mutableToken, TokenType.KeywordMutable))
		{
			if (Match(tokens, ref position, TokenType.KeywordRef))
			{
				var mutRefType = ParseType(tokens, ref position);
				return new ReferenceType(mutRefType, true, mutableToken.Range.Join(mutRefType.range));
			}
			
			var mutType = ParseType(tokens, ref position);
			return new MutableType(mutType, mutableToken.Range.Join(mutType.range));
		}
		
		if (Match(tokens, ref position, out var refToken, TokenType.KeywordRef))
		{
			var refType = ParseType(tokens, ref position);
			return new ReferenceType(refType, false, refToken.Range.Join(refType.range));
		}
		
		// Recursive types
		Type type = ParseBaseType(tokens, ref position);
		while (true)
		{
			// type[type1, type2, ...] or type[]
			if (Match(tokens, ref position, TokenType.OpLeftBracket))
			{
				if (Match(tokens, ref position, out var arrayRightBracket, TokenType.OpRightBracket))
				{
					type = new ArrayType(type, null, type.range.Join(arrayRightBracket.Range));
					continue;
				}

				try
				{
					var typeParameters = ParseTypeList(tokens, ref position);
					var genericRightBracket = Consume(tokens, ref position, null, TokenType.OpRightBracket);
					type = new GenericType(type, typeParameters, type.range.Join(genericRightBracket.Range));
					continue;
				}
				catch (ParseException)
				{
					// Try again as type[expression] for array allocation
					var expression = ParseExpression(tokens, ref position);
					var rightBracket = Consume(tokens, ref position, null, TokenType.OpRightBracket);
					type = new ArrayType(type, expression, type.range.Join(rightBracket.Range));
					continue;
				}
			}

			if (Match(tokens, ref position, out var question, TokenType.OpQuestion))
			{
				type = new NullableType(type, type.range.Join(question.Range));
				continue;
			}
			
			break;
		}

		return type;
	}

	private static BaseType ParseBaseType(IReadOnlyList<Token> tokens, ref int position)
	{
		var token = Consume(tokens, ref position, "Invalid token for type", TokenType.ValidDataTypes.ToArray());
		return new BaseType(token);
	}

	private static LambdaType ParseLambdaType(IReadOnlyList<Token> tokens, ref int position)
	{
		var startToken = Consume(tokens, ref position, null, TokenType.KeywordLambda);
		Consume(tokens, ref position, null, TokenType.OpLeftParen);

		var parameterTypes = new List<Type>();
		if (Peek(tokens, position) != TokenType.OpRightParen)
		{
			parameterTypes.AddRange(ParseTypeList(tokens, ref position));
		}

		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		Type? returnType = null;
		if (Match(tokens, ref position, TokenType.OpReturnType))
			returnType = ParseType(tokens, ref position);

		var range = startToken.Range.Join(endToken.Range);
		if (returnType is not null)
			range = range.Join(returnType.range);

		return new LambdaType(parameterTypes, returnType, range);
	}

	private static List<Type> ParseTypeList(IReadOnlyList<Token> tokens, ref int position)
	{
		var types = new List<Type>();
		
		do
		{
			types.Add(ParseType(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));

		return types;
	}

	private static Type ParseTupleType(IReadOnlyList<Token> tokens, ref int position)
	{
		var types = new List<Type>();
		var startToken = Consume(tokens, ref position, null, TokenType.OpLeftParen);

		do
		{
			types.Add(ParseType(tokens, ref position));
		} while (Match(tokens, ref position, TokenType.OpComma));
		
		var endToken = Consume(tokens, ref position, null, TokenType.OpRightParen);

		// If the tuple has only 1 value, it is actually a parenthesized type, not a tuple
		if (types.Count == 1)
			return types[0];

		return new TupleType(types, startToken.Range.Join(endToken.Range));
	}
}