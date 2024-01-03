using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public abstract class TypeSymbol : ISymbol
{
	private static uint nextID = 1u;
	
	public Type EvaluatedType => new BaseType(this);
	public abstract string SymbolTypeName { get; }
	public string Name { get; }
	public uint TypeID { get; }
	public List<TypeSymbol> Implementations { get; } = [];
	
	protected TypeSymbol(string name, bool increment = true)
	{
		Name = name;
		TypeID = increment ? nextID++ : 0u;
	}

	public static readonly NativeSymbol Int8 = new(TokenType.KeywordInt8.ToString());
	public static readonly NativeSymbol UInt8 = new(TokenType.KeywordUInt8.ToString());
	public static readonly NativeSymbol Int16 = new(TokenType.KeywordInt16.ToString());
	public static readonly NativeSymbol UInt16 = new(TokenType.KeywordUInt16.ToString());
	public static readonly NativeSymbol Int32 = new(TokenType.KeywordInt32.ToString());
	public static readonly NativeSymbol UInt32 = new(TokenType.KeywordUInt32.ToString());
	public static readonly NativeSymbol Int64 = new(TokenType.KeywordInt64.ToString());
	public static readonly NativeSymbol UInt64 = new(TokenType.KeywordUInt64.ToString());
	public static readonly NativeSymbol Int128 = new(TokenType.KeywordInt128.ToString());
	public static readonly NativeSymbol UInt128 = new(TokenType.KeywordUInt128.ToString());
	public static readonly AliasedSymbol Int = new(TokenType.KeywordInt.ToString(), Int32);
	public static readonly AliasedSymbol UInt = new(TokenType.KeywordUInt.ToString(), UInt32);
	public static readonly NativeSymbol Float32 = new(TokenType.KeywordFloat32.ToString());
	public static readonly NativeSymbol Float64 = new(TokenType.KeywordFloat64.ToString());
	public static readonly NativeSymbol Float128 = new(TokenType.KeywordFloat128.ToString());
	public static readonly AliasedSymbol Float = new(TokenType.KeywordFloat.ToString(), Float32);
	public static readonly NativeSymbol Fixed32 = new(TokenType.KeywordFixed32.ToString());
	public static readonly NativeSymbol Fixed64 = new(TokenType.KeywordFixed64.ToString());
	public static readonly NativeSymbol Fixed128 = new(TokenType.KeywordFixed128.ToString());
	public static readonly AliasedSymbol Fixed = new(TokenType.KeywordFixed.ToString(), Fixed64);
	public static readonly NativeSymbol Char = new(TokenType.KeywordChar.ToString());
	public static readonly NativeSymbol String = new(TokenType.KeywordString.ToString());
	public static readonly NativeSymbol Bool = new(TokenType.KeywordBool.ToString());
	public static readonly GenericNativeSymbol Array = new("$Array");
	public static readonly GenericNativeSymbol Nullable = new("$Nullable");
}

public class NativeSymbol : TypeSymbol
{
	public override string SymbolTypeName => "a native type";
	
	public NativeSymbol(string name) : base(name)
	{
	}
}

public sealed class GenericNativeSymbol : NativeSymbol
{
	public GenericNativeSymbol(string name) : base(name)
	{
	}
}

public sealed class StructSymbol : TypeSymbol
{
	public override string SymbolTypeName => "a struct";
	public bool IsMutable { get; }
	public Scope Scope { get; }
	
	public StructSymbol(string name, bool isMutable, Scope scope) : base(name)
	{
		IsMutable = isMutable;
		Scope = scope;
	}
}

public sealed class TypeParameterSymbol : TypeSymbol
{
	public override string SymbolTypeName => "a type parameter";
	
	public TypeParameterSymbol(string name) : base(name, false)
	{
	}
}