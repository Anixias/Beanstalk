using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public abstract class Type;

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
		return $"({string.Join(',', types.Select(t => t.ToString()))})";
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
		return $"{baseType}[{string.Join(',', typeParameters.Select(t => t.ToString()))}]";
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
		return $"({string.Join(',', parameterTypes.Select(p => p.ToString()))}) => {returnString}";
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
}