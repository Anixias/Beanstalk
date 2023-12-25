using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class Type : ExpressionNode
{
	protected Type(TextRange range) : base(range)
	{
		
	}
	
	public static Type? TryConvert(ExpressionNode expression)
	{
		switch (expression)
		{
			case TokenExpression tokenExpression:
				if (!TokenType.ValidDataTypes.Contains(tokenExpression.token.Type))
					return null;
				
				return new BaseType(tokenExpression.token);
			case IndexExpression indexExpression:
				var source = TryConvert(indexExpression.source);
				if (source is null)
					return null;
				
				var typeParameter = TryConvert(indexExpression.index);
				if (typeParameter is null)
					return null;

				return new GenericType(source, [typeParameter], indexExpression.range);
			default:
				return null;
		}
	}
}

public abstract class WrapperType : Type
{
	public readonly Type baseType;

	protected WrapperType(Type baseType, TextRange range) : base(range)
	{
		this.baseType = baseType;
	}
}

public sealed class TupleType : Type
{
	public readonly ImmutableArray<Type> types;

	public TupleType(IEnumerable<Type> types, TextRange range) : base(range)
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
	
	public GenericType(Type baseType, IEnumerable<Type> typeParameters, TextRange range) : base(baseType, range)
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
	public MutableType(Type baseType, TextRange range) : base(baseType, range)
	{
	}

	public override string ToString()
	{
		return $"mutable {baseType}";
	}
}

public sealed class ArrayType : WrapperType
{
	public readonly ExpressionNode? size;
	
	public ArrayType(Type baseType, ExpressionNode? size, TextRange range) : base(baseType, range)
	{
		this.size = size;
	}

	public override string ToString()
	{
		if (size is null)
			return $"{baseType}[]";
		
        return $"{baseType}[{size}]";
	}
}

public sealed class NullableType : WrapperType
{
	public NullableType(Type baseType, TextRange range) : base(baseType, range)
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

	public LambdaType(IEnumerable<Type> parameterTypes, Type? returnType, TextRange range) : base(range)
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

	public ReferenceType(Type baseType, bool immutable, TextRange range) : base(baseType, range)
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
	public readonly Token token;

	public BaseType(Token token) : base(token.Range)
	{
		this.token = token;
	}

	public override string ToString()
	{
		return token.Text;
	}
}