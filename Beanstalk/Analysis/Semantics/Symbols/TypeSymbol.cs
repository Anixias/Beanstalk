using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public abstract class TypeSymbol : ISymbol
{
	private static uint nextID = 1u;
	private uint nextFieldIndex;
	
	public Type EvaluatedType => new BaseType(this);
	public abstract string SymbolTypeName { get; }
	public string Name { get; }
	public uint TypeID { get; }
	public List<TypeSymbol> Implementations { get; } = [];
	public SymbolTable SymbolTable { get; }
	public List<OperatorOverloadSymbol> Operators { get; } = [];
	public bool IsMutable { get; }
	public bool HasStaticFields { get; set; }
	
	protected TypeSymbol(string name, bool isMutable, SymbolTable symbolTable, bool increment = true)
	{
		Name = name;
		IsMutable = isMutable;
		SymbolTable = symbolTable;
		TypeID = increment ? nextID++ : 0u;
	}

	public uint NextFieldIndex()
	{
		return nextFieldIndex++;
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

	static TypeSymbol()
	{
		BuildNumeric(Int8);
		BuildNumeric(Int16);
		BuildNumeric(Int32);
		BuildNumeric(Int64);
		BuildNumeric(Int128);
		
		BuildNumeric(UInt8);
		BuildNumeric(UInt16);
		BuildNumeric(UInt32);
		BuildNumeric(UInt64);
		BuildNumeric(UInt128);
		
		BuildNumeric(Fixed32);
		BuildNumeric(Fixed64);
		BuildNumeric(Fixed128);
		
		BuildNumeric(Float32);
		BuildNumeric(Float64);
		BuildNumeric(Float128);
		
		BuildAdd(String);
	}

	private static void BuildNumeric(TypeSymbol type)
	{
		BuildAdd(type);
		BuildMultiply(type);
	}

	private static void BuildAdd(TypeSymbol type)
	{
		BuildOperator(type, type.EvaluatedType, BinaryExpression.Operation.Add, type.EvaluatedType, type.EvaluatedType);
	}

	private static void BuildMultiply(TypeSymbol type)
	{
		BuildOperator(type, type.EvaluatedType, BinaryExpression.Operation.Multiply, type.EvaluatedType,
			type.EvaluatedType);
	}

	private static void BuildOperator(TypeSymbol typeSymbol, Type left, BinaryExpression.Operation operation,
		Type right, Type returnType)
	{
		var leftVar = new VarSymbol("$left", false)
		{
			EvaluatedType = left
		};
		
		var rightVar = new VarSymbol("$right", false)
		{
			EvaluatedType = right
		};

		var leftSymbol = new ParameterSymbol(leftVar, null, false, 0u);
		var rightSymbol = new ParameterSymbol(rightVar, null, false, 1u);

		var operatorSymbol =
			new BinaryOperatorOverloadSymbol(leftSymbol, operation, rightSymbol, returnType, new Scope(), true);
		
		typeSymbol.SymbolTable.Add(operatorSymbol);
		typeSymbol.Operators.Add(operatorSymbol);
	}

	private static void BuildOperator(TypeSymbol typeSymbol, UnaryExpression.Operation operation, Type operand,
		Type returnType)
	{
		var operandVar = new VarSymbol("$operand", false)
		{
			EvaluatedType = operand
		};

		var operandSymbol = new ParameterSymbol(operandVar, null, false, 0u);

		var operatorSymbol =
			new UnaryOperatorOverloadSymbol(operandSymbol, operation, returnType, new Scope(), true);
		
		typeSymbol.SymbolTable.Add(operatorSymbol);
		typeSymbol.Operators.Add(operatorSymbol);
	}

	public BinaryOperatorOverloadSymbol? FindOperator(Type? leftType, Type? rightType,
		BinaryExpression.Operation operation)
	{
		foreach (var operatorOverload in Operators)
		{
			if (operatorOverload is not BinaryOperatorOverloadSymbol symbol)
				continue;

			if (symbol.Operation != operation)
				continue;
			
			// Todo: Handle implicit casts (1-level deep)

			if (!Type.Matches(symbol.Left.EvaluatedType, leftType))
				continue;

			if (!Type.Matches(symbol.Right.EvaluatedType, rightType))
				continue;

			return symbol;
		}

		return null;
	}

	public UnaryOperatorOverloadSymbol? FindOperator(Type? operandType, UnaryExpression.Operation operation)
	{
		foreach (var operatorOverload in Operators)
		{
			if (operatorOverload is not UnaryOperatorOverloadSymbol symbol)
				continue;

			if (symbol.Operation != operation)
				continue;
			
			// Todo: Handle implicit casts (1-level deep)

			if (!Type.Matches(symbol.Operand.EvaluatedType, operandType))
				continue;

			return symbol;
		}

		return null;
	}
}

public class NativeSymbol : TypeSymbol
{
	public override string SymbolTypeName => "a native type";
	
	public NativeSymbol(string name) : base(name, false, new SymbolTable())
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
	public Scope Scope { get; }
	
	public StructSymbol(string name, bool isMutable, Scope scope) : base(name, isMutable, scope.SymbolTable)
	{
		Scope = scope;
	}
}

public sealed class TypeParameterSymbol : TypeSymbol
{
	public override string SymbolTypeName => "a type parameter";
	
	public TypeParameterSymbol(string name) : base(name, true, new SymbolTable(), false)
	{
	}
}