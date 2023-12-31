using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public abstract class Type
{
	public abstract bool Equals(Type? type);
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
		if (type is not NullableType nullableType)
			return false;

		if (!baseType.Equals(nullableType.baseType))
			return false;

		return true;
	}
}

public sealed class LambdaType : Type
{
	public readonly ImmutableArray<Type> parameterTypes;
	public readonly Type? returnType;

	public LambdaType(IEnumerable<Type> parameterTypes, Type? returnType)
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
		if (type is not LambdaType lambdaType)
			return false;

		if (returnType is null != lambdaType.returnType is null)
			return false;

		if (returnType?.Equals(lambdaType.returnType!) == false)
			return false;
		
		if (parameterTypes.Length != lambdaType.parameterTypes.Length)
			return false;

		for (var i = 0; i < parameterTypes.Length; i++)
		{
			if (!parameterTypes[i].Equals(lambdaType.parameterTypes[i]))
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