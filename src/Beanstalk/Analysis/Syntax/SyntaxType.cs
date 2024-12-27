using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class SyntaxType : ExpressionNode
{
	protected SyntaxType(TextRange range) : base(range)
	{
		
	}
	
	public static SyntaxType? TryConvert(ExpressionNode expression)
	{
		switch (expression)
		{
			case TokenExpression tokenExpression:
				if (!TokenType.ValidDataTypes.Contains(tokenExpression.token.Type))
					return null;
				
				return new BaseSyntaxType(tokenExpression.token);
			case IndexExpression indexExpression:
				var source = TryConvert(indexExpression.source);
				if (source is null)
					return null;
				
				var typeParameter = TryConvert(indexExpression.index);
				if (typeParameter is null)
					return null;

				return new GenericSyntaxType(source, [typeParameter], indexExpression.range);
			default:
				return null;
		}
	}

	public override void Accept(ExpressionNode.IVisitor visitor)
	{
		throw new NotImplementedException();
	}

	public override T Accept<T>(ExpressionNode.IVisitor<T> visitor)
	{
		throw new NotImplementedException();
	}
	
	public new interface IVisitor<out T>
	{
		T Visit(TupleSyntaxType syntaxType);
		T Visit(GenericSyntaxType syntaxType);
		T Visit(MutableSyntaxType syntaxType);
		T Visit(ArraySyntaxType syntaxType);
		T Visit(NullableSyntaxType syntaxType);
		T Visit(LambdaSyntaxType syntaxType);
		T Visit(ReferenceSyntaxType syntaxType);
		T Visit(BaseSyntaxType syntaxType);
	}
	
	public new interface IVisitor
	{
		void Visit(TupleSyntaxType syntaxType);
		void Visit(GenericSyntaxType syntaxType);
		void Visit(MutableSyntaxType syntaxType);
		void Visit(ArraySyntaxType syntaxType);
		void Visit(NullableSyntaxType syntaxType);
		void Visit(LambdaSyntaxType syntaxType);
		void Visit(ReferenceSyntaxType syntaxType);
		void Visit(BaseSyntaxType syntaxType);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
}

public abstract class WrapperSyntaxType : SyntaxType
{
	public readonly SyntaxType baseSyntaxType;

	protected WrapperSyntaxType(SyntaxType baseSyntaxType, TextRange range) : base(range)
	{
		this.baseSyntaxType = baseSyntaxType;
	}
}

public sealed class TupleSyntaxType : SyntaxType
{
	public readonly ImmutableArray<SyntaxType> types;

	public TupleSyntaxType(IEnumerable<SyntaxType> types, TextRange range) : base(range)
	{
		this.types = types.ToImmutableArray();
	}

	public override string ToString()
	{
		return $"({string.Join(',', types.Select(t => t.ToString()))})";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class GenericSyntaxType : WrapperSyntaxType
{
	public readonly ImmutableArray<SyntaxType> typeParameters;
	
	public GenericSyntaxType(SyntaxType baseSyntaxType, IEnumerable<SyntaxType> typeParameters, TextRange range)
		: base(baseSyntaxType, range)
	{
		this.typeParameters = typeParameters.ToImmutableArray();
	}

	public override string ToString()
	{
		return $"{baseSyntaxType}[{string.Join(',', typeParameters.Select(t => t.ToString()))}]";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class MutableSyntaxType : WrapperSyntaxType
{
	public MutableSyntaxType(SyntaxType baseSyntaxType, TextRange range) : base(baseSyntaxType, range)
	{
	}

	public override string ToString()
	{
		return $"mutable {baseSyntaxType}";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ArraySyntaxType : WrapperSyntaxType
{
	public readonly ExpressionNode? size;
	
	public ArraySyntaxType(SyntaxType baseSyntaxType, ExpressionNode? size, TextRange range) : base(baseSyntaxType, range)
	{
		this.size = size;
	}

	public override string ToString()
	{
		if (size is null)
			return $"{baseSyntaxType}[]";
		
        return $"{baseSyntaxType}[{size}]";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class NullableSyntaxType : WrapperSyntaxType
{
	public NullableSyntaxType(SyntaxType baseSyntaxType, TextRange range) : base(baseSyntaxType, range)
	{
	}

	public override string ToString()
	{
		return $"{baseSyntaxType}?";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class LambdaSyntaxType : SyntaxType
{
	public readonly ImmutableArray<SyntaxType> parameterTypes;
	public readonly SyntaxType? returnType;

	public LambdaSyntaxType(IEnumerable<SyntaxType> parameterTypes, SyntaxType? returnType, TextRange range) : base(range)
	{
		this.parameterTypes = parameterTypes.ToImmutableArray();
		this.returnType = returnType;
	}

	public override string ToString()
	{
		var returnString = returnType is null ? "." : $"{returnType}";
		return $"({string.Join(',', parameterTypes.Select(p => p.ToString()))}) => {returnString}";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ReferenceSyntaxType : WrapperSyntaxType
{
	public readonly bool immutable;

	public ReferenceSyntaxType(SyntaxType baseSyntaxType, bool immutable, TextRange range) : base(baseSyntaxType, range)
	{
		this.immutable = immutable;
	}

	public override string ToString()
	{
		return immutable ? $"ref {baseSyntaxType}" : $"mutable ref {baseSyntaxType}";
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class BaseSyntaxType : SyntaxType
{
	public readonly Token token;

	public BaseSyntaxType(Token token) : base(token.Range)
	{
		this.token = token;
	}

	public override string ToString()
	{
		return token.Text;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}