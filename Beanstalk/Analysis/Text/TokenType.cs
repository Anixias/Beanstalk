using System.Collections.Immutable;

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

	public static readonly TokenType MultilineComment = new("block comment")
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

	public static readonly TokenType InterpolatedStringLiteral = new("interpolated string literal")
	{
		IsLiteral = true
	};

	public static readonly TokenType InvalidStringLiteral = new("invalid string literal")
	{
		IsLiteral = true,
		IsInvalid = true
	};

	public static readonly TokenType CharLiteral = new("char literal")
	{
		IsLiteral = true
	};

	public static readonly TokenType InvalidCharLiteral = new("invalid char literal")
	{
		IsLiteral = true,
		IsInvalid = true
	};
	
	public static readonly TokenType KeywordImport = CreateKeyword("import");
	public static readonly TokenType KeywordModule = CreateKeyword("module");
	public static readonly TokenType KeywordEntry = CreateKeyword("entry");
	public static readonly TokenType KeywordLet = CreateKeyword("let");
	public static readonly TokenType KeywordVar = CreateKeyword("var");
	public static readonly TokenType KeywordConst = CreateKeyword("const");
	public static readonly TokenType KeywordTrue = CreateKeyword("true");
	public static readonly TokenType KeywordFalse = CreateKeyword("false");
	public static readonly TokenType KeywordNull = CreateKeyword("null");
	public static readonly TokenType KeywordThis = CreateKeyword("this");
	public static readonly TokenType KeywordMutable = CreateKeyword("mutable");
	public static readonly TokenType KeywordLambda = CreateKeyword("lambda");
	public static readonly TokenType KeywordAs = CreateKeyword("as");
	public static readonly TokenType KeywordIs = CreateKeyword("is");
	public static readonly TokenType KeywordRef = CreateKeyword("ref");
	public static readonly TokenType KeywordSwitch = CreateKeyword("switch");
	public static readonly TokenType KeywordWith = CreateKeyword("with");
	public static readonly TokenType KeywordAwait = CreateKeyword("await");
	public static readonly TokenType KeywordFun = CreateKeyword("fun");
	public static readonly TokenType KeywordReturn = CreateKeyword("return");
	public static readonly TokenType KeywordIf = CreateKeyword("if");
	public static readonly TokenType KeywordElse = CreateKeyword("else");
	public static readonly TokenType KeywordNew = CreateKeyword("new");
	public static readonly TokenType KeywordStatic = CreateKeyword("static");
	public static readonly TokenType KeywordConstructor = CreateKeyword("constructor");
	public static readonly TokenType KeywordDestructor = CreateKeyword("destructor");
	public static readonly TokenType KeywordStruct = CreateKeyword("struct");
	public static readonly TokenType KeywordInterface = CreateKeyword("interface");
	public static readonly TokenType KeywordImplicit = CreateKeyword("implicit");
	public static readonly TokenType KeywordExplicit = CreateKeyword("explicit");
	public static readonly TokenType KeywordCast = CreateKeyword("cast");
	public static readonly TokenType KeywordOperator = CreateKeyword("operator");
	
	// Native data types
	public static readonly TokenType KeywordInt = CreateKeyword("int");
	public static readonly TokenType KeywordUInt = CreateKeyword("uint");
	public static readonly TokenType KeywordNInt = CreateKeyword("nint");
	public static readonly TokenType KeywordNUInt = CreateKeyword("nuint");
	public static readonly TokenType KeywordInt8 = CreateKeyword("int8");
	public static readonly TokenType KeywordUInt8 = CreateKeyword("uint8");
	public static readonly TokenType KeywordInt16 = CreateKeyword("int16");
	public static readonly TokenType KeywordUInt16 = CreateKeyword("uint16");
	public static readonly TokenType KeywordInt32 = CreateKeyword("int32");
	public static readonly TokenType KeywordUInt32 = CreateKeyword("uint32");
	public static readonly TokenType KeywordInt64 = CreateKeyword("int64");
	public static readonly TokenType KeywordUInt64 = CreateKeyword("uint64");
	public static readonly TokenType KeywordInt128 = CreateKeyword("int128");
	public static readonly TokenType KeywordUInt128 = CreateKeyword("uint128");
	public static readonly TokenType KeywordSingle = CreateKeyword("single");
	public static readonly TokenType KeywordDouble = CreateKeyword("double");
	public static readonly TokenType KeywordQuad = CreateKeyword("quad");
	public static readonly TokenType KeywordCoarse = CreateKeyword("coarse");
	public static readonly TokenType KeywordFixed = CreateKeyword("fixed");
	public static readonly TokenType KeywordPrecise = CreateKeyword("precise");
	public static readonly TokenType KeywordBool = CreateKeyword("bool");
	public static readonly TokenType KeywordString = CreateKeyword("string");
	public static readonly TokenType KeywordChar = CreateKeyword("char");
	
	public static readonly ImmutableArray<TokenType> NativeDataTypes = new[]
	{
		KeywordInt, KeywordUInt,
		KeywordNInt, KeywordNUInt,
		KeywordInt8, KeywordUInt8,
		KeywordInt16, KeywordUInt16,
		KeywordInt32, KeywordUInt32,
		KeywordInt64, KeywordUInt64,
		KeywordInt128, KeywordUInt128,
		KeywordSingle, KeywordDouble,
		KeywordQuad, KeywordFixed, KeywordCoarse,
		KeywordPrecise, KeywordBool,
		KeywordString, KeywordChar
	}.ToImmutableArray();

	public static readonly ImmutableArray<TokenType> ValidDataTypes =
		NativeDataTypes.Append(Identifier).ToImmutableArray();

	public static TokenType OpReturnType => OpColonRight;
	public static readonly TokenType OpColonColon = CreateOperator("::");
	public static readonly TokenType OpDoubleArrow = CreateOperator("=>");
	public static readonly TokenType OpColon = CreateOperator(":");
	public static readonly TokenType OpColonRight = CreateOperator(":>");
	public static readonly TokenType OpSemicolon = CreateOperator(";");
	public static readonly TokenType OpLeftParen = CreateOperator("(");
	public static readonly TokenType OpRightParen = CreateOperator(")");
	public static readonly TokenType OpHashLeftBracket = CreateOperator("#[");
	public static readonly TokenType OpQuestionLeftBracket = CreateOperator("?[");
	public static readonly TokenType OpLeftBracket = CreateOperator("[");
	public static readonly TokenType OpRightBracket = CreateOperator("]");
	public static readonly TokenType OpLeftBrace = CreateOperator("{");
	public static readonly TokenType OpRightBrace = CreateOperator("}");
	public static readonly TokenType OpLessEqual = CreateOperator("<=");
	public static readonly TokenType OpRotLeft = CreateOperator("<<<");
	public static readonly TokenType OpLeftLeft = CreateOperator("<<");
	public static readonly TokenType OpLess = CreateOperator("<");
	public static readonly TokenType OpGreaterEqual = CreateOperator(">=");
	public static readonly TokenType OpRotRight = CreateOperator(">>>");
	public static readonly TokenType OpRightRight = CreateOperator(">>");
	public static readonly TokenType OpGreater = CreateOperator(">");
	public static readonly TokenType OpComma = CreateOperator(",");
	public static readonly TokenType OpDotDotEqual = CreateOperator("..=");
	public static readonly TokenType OpDotDot = CreateOperator("..");
	public static readonly TokenType OpQuestionDot = CreateOperator("?.");
	public static readonly TokenType OpDot = CreateOperator(".");
	public static readonly TokenType OpEqualsEquals = CreateOperator("==");
	public static readonly TokenType OpBangEquals = CreateOperator("!=");
	public static readonly TokenType OpBang = CreateOperator("!");
	public static readonly TokenType OpEquals = CreateOperator("=");
	public static readonly TokenType OpPlusPlus = CreateOperator("++");
	public static readonly TokenType OpPlus = CreateOperator("+");
	public static readonly TokenType OpMinusMinus = CreateOperator("--");
	public static readonly TokenType OpMinus = CreateOperator("-");
	public static readonly TokenType OpTilde = CreateOperator("~");
	public static readonly TokenType OpStarStar = CreateOperator("**");
	public static readonly TokenType OpStar = CreateOperator("*");
	public static readonly TokenType OpSlash = CreateOperator("/");
	public static readonly TokenType OpPercentPercent = CreateOperator("%%");
	public static readonly TokenType OpPercent = CreateOperator("%");
	public static readonly TokenType OpQuestionQuestion = CreateOperator("??");
	public static readonly TokenType OpQuestion = CreateOperator("?");
	public static readonly TokenType OpBarBar = CreateOperator("||");
	public static readonly TokenType OpBar = CreateOperator("|");
	public static readonly TokenType OpAmpAmp = CreateOperator("&&");
	public static readonly TokenType OpAmp = CreateOperator("&");
	public static readonly TokenType OpHatHat = CreateOperator("^^");
	public static readonly TokenType OpHat = CreateOperator("^");

	public static TokenType? GetKeyword(string keyword)
	{
		return Keywords.GetValueOrDefault(keyword);
	}

	public static TokenType? GetOperator(string @operator)
	{
		return Operators.GetValueOrDefault(@operator);
	}
}