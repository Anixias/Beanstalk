namespace Beanstalk.Analysis.Text;

public sealed class TokenType
{
	public bool IsInvalid { get; private init; }
	public bool IsKeyword { get; private init; }
	public bool IsIdentifier { get; private init; }
	public bool IsOperator { get; private init; }
	public bool IsLiteral { get; private init; }
	public bool IsFiltered { get; private init; }
	
	private static readonly Dictionary<string, TokenType> Keywords = new();
	private static readonly Dictionary<string, TokenType> Operators = new();

	private readonly string representation;

	private TokenType(string representation)
	{
		this.representation = representation;
	}

	public override string ToString() => representation;

	private static TokenType CreateKeyword(string text)
	{
		var type = new TokenType(text)
		{
			IsKeyword = true
		};

		Keywords.Add(text, type);
		return type;
	}
	
	private static TokenType CreateOperator(string text)
	{
		var type = new TokenType(text)
		{
			IsOperator = true
		};

		Operators.Add(text, type);
		return type;
	}

	public static readonly TokenType EndOfFile = new("eof")
	{
		IsInvalid = true
	};
	
	public static readonly TokenType Identifier = new("identifier")
	{
		IsIdentifier = true
	};

	public static readonly TokenType Invalid = new("invalid")
	{
		IsInvalid = true
	};

	public static readonly TokenType Whitespace = new("whitespace")
	{
		IsFiltered = true
	};

	public static readonly TokenType LineComment = new("line comment")
	{
		IsFiltered = true
	};

	public static readonly TokenType BlockComment = new("block comment")
	{
		IsFiltered = true
	};

	public static readonly TokenType DocumentationComment = new("documentation comment")
	{
		IsFiltered = true
	};

	public static readonly TokenType NumberLiteral = new("number literal")
	{
		IsLiteral = true
	};

	public static readonly TokenType InvalidNumberLiteral = new("invalid number literal")
	{
		IsLiteral = true,
		IsInvalid = true
	};

	public static readonly TokenType StringLiteral = new("string literal")
	{
		IsLiteral = true
	};

	public static readonly TokenType CharLiteral = new("char literal")
	{
		IsLiteral = true
	};
	
	public static readonly TokenType KeywordImport = CreateKeyword("import");
	public static readonly TokenType KeywordModule = CreateKeyword("module");
	public static readonly TokenType KeywordEntry = CreateKeyword("entry");
	public static readonly TokenType KeywordLet = CreateKeyword("let");
	public static readonly TokenType KeywordVar = CreateKeyword("var");

	public static readonly TokenType OpColon = CreateOperator(":");
	public static readonly TokenType OpSemicolon = CreateOperator(";");
	public static readonly TokenType OpLeftParen = CreateOperator("(");
	public static readonly TokenType OpRightParen = CreateOperator(")");
	public static readonly TokenType OpLeftBracket = CreateOperator("[");
	public static readonly TokenType OpRightBracket = CreateOperator("]");
	public static readonly TokenType OpLeftBrace = CreateOperator("{");
	public static readonly TokenType OpRightBrace = CreateOperator("}");
	public static readonly TokenType OpLeftAngled = CreateOperator("<");
	public static readonly TokenType OpRightAngled = CreateOperator(">");
	public static readonly TokenType OpComma = CreateOperator(",");
	public static readonly TokenType OpDot = CreateOperator(".");
	public static readonly TokenType OpEquals = CreateOperator("=");
	public static readonly TokenType OpPlus = CreateOperator("+");
	public static readonly TokenType OpMinus = CreateOperator("-");
	public static readonly TokenType OpStarStar = CreateOperator("**");
	public static readonly TokenType OpStar = CreateOperator("*");
	public static readonly TokenType OpDivide = CreateOperator("/");
	public static readonly TokenType OpQuestion = CreateOperator("?");

	public static TokenType? GetKeyword(string keyword)
	{
		return Keywords.GetValueOrDefault(keyword);
	}

	public static TokenType? GetOperator(string @operator)
	{
		return Operators.GetValueOrDefault(@operator);
	}
}