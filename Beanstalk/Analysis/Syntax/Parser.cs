using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

internal class ParseException : Exception
{
	public ParseException(string? message)
	: base(FormatMessage(message, null, null))
	{
	}
	
	public ParseException(string? message, Token? token)
	: base(FormatMessage(message, token, null))
	{
	}
	
	public ParseException(string? message, Token? token, TextRange range)
	: base(FormatMessage(message, token, range))
	{
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
		
		return $"[{token.Line}:{token.Column} at '{text}'] {message}";
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
			var startToken = TokenAt(tokens, position);
			var imports = ParseImportStatements(tokens, ref position);
			TryParseModuleStatement(tokens, ref position, false, out var module);
			var statements = ParseTopLevelStatements(tokens, ref position);

			if (Peek(tokens, position) != TokenType.EndOfFile)
				throw new ParseException("Expected 'end of file'", TokenAt(tokens, position));

			var range = startToken is null
				? new TextRange(0, 0)
				: new TextRange(0, startToken.Source.Length - 1);

			return new ProgramStatement(imports, module, statements, range);
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
				throw new ParseException("Invalid module statement: Expected 'identifier'", token);
		}

		var statements = new List<StatementNode>();
		if (Match(tokens, ref position, TokenType.OpLeftBrace))
		{
			statements.AddRange(ParseTopLevelStatements(tokens, ref position));
			var endToken = Consume(tokens, ref position, "Expected statement", TokenType.OpRightBrace);
			range = range.Join(endToken.Range);
		}
		else if (requireBody)
			throw new ParseException("Module statement must have a body", TokenAt(tokens, position - 1));

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
				parameters.Add(ParseParameter(tokens, ref position));
			} while (Match(tokens, ref position, TokenType.OpComma));
		}
		
		Consume(tokens, ref position, null, TokenType.OpRightParen);

		var body = ParseBlockStatement(tokens, ref position);
		return new EntryStatement(parameters, body, startToken.Range.Join(body.range));
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
			
			if (TryParseEntryStatement(tokens, ref position, out var entryStatement))
			{
				statements.Add(entryStatement);
				continue;
			}

			break;
		}
		
		return statements;
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
			if (TryParseVarDeclaration(tokens, ref position, out var varDeclaration))
			{
				statements.Add(varDeclaration);
				continue;
			}

			if (!IsEndOfFile(tokens, position))
			{
				var expression = ParseExpression(tokens, ref position);
				var expressionStatement = new ExpressionStatement(expression, expression.range);
				statements.Add(expressionStatement);
				continue;
			}

			break;
		}
		
		return statements;
	}

	private static bool TryParseVarDeclaration(IReadOnlyList<Token> tokens, ref int position,
		[NotNullWhen(true)] out VarDeclarationStatement? varDeclaration)
	{
		varDeclaration = null;
		var peek = Peek(tokens, position);

		if (peek != TokenType.KeywordLet && peek != TokenType.KeywordVar && peek != TokenType.KeywordConst)
			return false;

		varDeclaration = ParseVarDeclaration(tokens, ref position);
		return true;
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

		throw new ParseException("Expected variable declaration");
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

	private static ExpressionNode ParsePrefixUnaryExpression(IReadOnlyList<Token> tokens, ref int position)
	{
		if (Match(tokens, ref position, out var op, TokenType.OpPlusPlus, TokenType.OpMinusMinus, TokenType.OpPlus,
			    TokenType.OpMinus, TokenType.OpTilde, TokenType.OpBang, TokenType.KeywordAwait))
		{
			var right = ParsePrefixUnaryExpression(tokens, ref position);
			var range = op.Range.Join(right.range);
			
			UnaryExpression.Operation operation;
			if (op.Type == TokenType.OpPlusPlus)
				operation = UnaryExpression.Operation.PreIncrement;
			else if (op.Type == TokenType.OpMinusMinus)
				operation = UnaryExpression.Operation.PreDecrement;
			else if (op.Type == TokenType.OpPlus)
				operation = UnaryExpression.Operation.Identity;
			else if (op.Type == TokenType.OpMinus)
				operation = UnaryExpression.Operation.Negate;
			else if (op.Type == TokenType.OpTilde)
				operation = UnaryExpression.Operation.BitwiseNegate;
			else if (op.Type == TokenType.OpBang)
				operation = UnaryExpression.Operation.LogicalNot;
			else if (op.Type == TokenType.KeywordAwait)
				operation = UnaryExpression.Operation.Await;
			else
				throw new ParseException("Unexpected operation", op);
			
			return new UnaryExpression(right, operation, true, range);
		}

		return ParsePostfixUnaryExpression(tokens, ref position);
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

		return new Parameter(identifier, type);
	}

	private static Parameter ParseParameter(IReadOnlyList<Token> tokens, ref int position)
	{
		var identifier = Consume(tokens, ref position, null, TokenType.Identifier);
		Consume(tokens, ref position, null, TokenType.OpColon);
		var type = ParseType(tokens, ref position);
		ExpressionNode? defaultExpression = null;
		if (Match(tokens, ref position, TokenType.OpEquals))
			defaultExpression = ParseExpression(tokens, ref position);
		
		return new Parameter(identifier, type, defaultExpression);
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
			if (Match(tokens, ref position, out var op, TokenType.OpPlusPlus, TokenType.OpMinusMinus))
			{
				UnaryExpression.Operation operation;
				if (op.Type == TokenType.OpPlusPlus)
					operation = UnaryExpression.Operation.PostIncrement;
				else if (op.Type == TokenType.OpMinusMinus)
					operation = UnaryExpression.Operation.PostDecrement;
				else
					throw new ParseException("Unexpected operation", op);
				
				expression = new UnaryExpression(expression, operation, false, expression.range.Join(op.Range));
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
			    TokenType.KeywordNull))
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
					type = new ArrayType(type, type.range.Join(arrayRightBracket.Range));
					continue;
				}

				var typeParameters = ParseTypeList(tokens, ref position);
				var genericRightBracket = Consume(tokens, ref position, null, TokenType.OpRightBracket);
				type = new GenericType(type, typeParameters, type.range.Join(genericRightBracket.Range));
				continue;
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