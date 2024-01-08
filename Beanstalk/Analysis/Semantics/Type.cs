using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public abstract class Type
{
	public abstract bool Equals(Type? type);

	public static bool Matches(Type? left, Type? right)
	{
		if (left is null)
			return right is null || right.Equals(left);
		
		return left.Equals(right);
	}

	public static Result<BinaryOperatorOverloadSymbol, string> FindOperator(Type? left, Type? right,
		BinaryExpression.Operation operation)
	{
		if (left is null && right is null)
			return Result<BinaryOperatorOverloadSymbol, string>.FromError("Operator not found");

		var leftResult = left switch
		{
			BaseType type => type.typeSymbol.FindOperator(left, right, operation),
			
			ArrayType type =>
				// Todo: Handle concrete implementations
				TypeSymbol.Array.FindOperator(left, right, operation),
			
			NullableType type =>
				// Todo: This is incorrect
				FindOperator(type.baseType, right, operation).result,
			
			_ => FindOperator(right, left, operation).result
		};

		BinaryOperatorOverloadSymbol? rightResult = null;

		if (!Matches(left, right))
		{
			rightResult = right switch
			{
				BaseType type => type.typeSymbol.FindOperator(left, right, operation),

				ArrayType type =>
					// Todo: Handle concrete implementations
					TypeSymbol.Array.FindOperator(left, right, operation),

				NullableType type =>
					// Todo: This is incorrect
					FindOperator(type.baseType, right, operation).result,

				_ => FindOperator(right, left, operation).result
			};
		}

		// Todo: Report more information about the ambiguity: The type that defines each, source file for each, etc.
		if (leftResult is not null)
		{
			if (rightResult is not null)
				return Result<BinaryOperatorOverloadSymbol, string>.FromError("Ambiguous invocation");
			
			return Result<BinaryOperatorOverloadSymbol, string>.FromSuccess(leftResult);
		}

		if (rightResult is not null)
			return Result<BinaryOperatorOverloadSymbol, string>.FromSuccess(rightResult);
		
		return Result<BinaryOperatorOverloadSymbol, string>.FromError("Operator not found");
	}
}

public abstract class WrapperType : Type
{
	public readonly Type baseType;

	protected WrapperType(Type baseType)
	{
		this.baseType = baseType;
	}
}

public sealed class TupleType : Type
{
	public readonly ImmutableArray<Type> types;

	public TupleType(IEnumerable<Type> types)
	{
		this.types = types.ToImmutableArray();
	}

	public override string ToString()
	{
		return $"({string.Join(", ", types.Select(t => t.ToString()))})";
	}

	public override bool Equals(Type? type)
	{
		if (type is not TupleType tupleType)
			return false;

		if (types.Length != tupleType.types.Length)
			return false;

		for (var i = 0; i < types.Length; i++)
		{
			if (!types[i].Equals(tupleType.types[i]))
				return false;
		}

		return true;
	}
}

public sealed class GenericType : WrapperType
{
	public readonly ImmutableArray<Type> typeParameters;
	
	public GenericType(Type baseType, IEnumerable<Type> typeParameters) : base(baseType)
	{
		this.typeParameters = typeParameters.ToImmutableArray();
	}

	public override string ToString()
	{
		return $"{baseType}[{string.Join(", ", typeParameters.Select(t => t.ToString()))}]";
	}

	public override bool Equals(Type? type)
	{
		if (type is not GenericType genericType)
			return false;

		if (typeParameters.Length != genericType.typeParameters.Length)
			return false;

		for (var i = 0; i < typeParameters.Length; i++)
		{
			if (!typeParameters[i].Equals(genericType.typeParameters[i]))
				return false;
		}

		return true;
	}
}

public sealed class MutableType : WrapperType
{
	public MutableType(Type baseType) : base(baseType)
	{
	}

	public override string ToString()
	{
		return $"mutable {baseType}";
	}

	public override bool Equals(Type? type)
	{
		if (type is not MutableType mutableType)
			return false;

		if (!baseType.Equals(mutableType.baseType))
			return false;

		return true;
	}
}

public sealed class ArrayType : WrapperType
{
	public ArrayType(Type baseType) : base(baseType)
	{
	}

	public override string ToString()
	{
		return $"{baseType}[]";
	}

	public override bool Equals(Type? type)
	{
		if (type is not ArrayType arrayType)
			return false;

		if (!baseType.Equals(arrayType.baseType))
			return false;

		return true;
	}
}

public sealed class NullableType : WrapperType
{
	public NullableType(Type baseType) : base(baseType)
	{
	}

	public override string ToString()
	{
		return $"{baseType}?";
	}

	public override bool Equals(Type? type)
	{
		if (type is null)
			return true;

		if (type is not NullableType nullableType)
			return baseType.Equals(type);
		
		if (!baseType.Equals(nullableType.baseType))
			return false;

		return true;
	}
}

public sealed class FunctionType : Type
{
	public readonly ImmutableArray<Type> parameterTypes;
	public readonly Type? returnType;

	public FunctionType(IEnumerable<Type> parameterTypes, Type? returnType)
	{
		this.parameterTypes = parameterTypes.ToImmutableArray();
		this.returnType = returnType;
	}

	public override string ToString()
	{
		var returnString = returnType is null ? "." : $"{returnType}";
		return $"({string.Join(", ", parameterTypes.Select(p => p.ToString()))}) => {returnString}";
	}

	public override bool Equals(Type? type)
	{
		if (type is not FunctionType functionType)
			return false;

		if (returnType is null != functionType.returnType is null)
			return false;

		if (returnType?.Equals(functionType.returnType!) == false)
			return false;
		
		if (parameterTypes.Length != functionType.parameterTypes.Length)
			return false;

		for (var i = 0; i < parameterTypes.Length; i++)
		{
			if (!parameterTypes[i].Equals(functionType.parameterTypes[i]))
				return false;
		}

		return true;
	}
}

public sealed class ReferenceType : WrapperType
{
	public readonly bool immutable;

	public ReferenceType(Type baseType, bool immutable) : base(baseType)
	{
		this.immutable = immutable;
	}

	public override string ToString()
	{
		return immutable ? $"ref {baseType}" : $"mutable ref {baseType}";
	}

	public override bool Equals(Type? type)
	{
		if (type is not ReferenceType referenceType)
			return false;

		if (!baseType.Equals(referenceType.baseType))
			return false;

		return true;
	}
}

public sealed class BaseType : Type
{
	public readonly TypeSymbol typeSymbol;

	public BaseType(TypeSymbol typeSymbol)
	{
		this.typeSymbol = typeSymbol;
	}

	public override string ToString()
	{
		return typeSymbol.Name;
	}

	public override bool Equals(Type? type)
	{
		if (type is not BaseType baseType)
			return false;

		if (typeSymbol != baseType.typeSymbol)
			return false;

		return true;
	}
}