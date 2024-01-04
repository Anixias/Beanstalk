using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public abstract class ResolvedExpressionNode : IResolvedAstNode
{
	public Type? Type { get; }

	protected ResolvedExpressionNode(Type? type)
	{
		Type = type;
	}
	public interface IVisitor<out T>
	{
		T Visit(ResolvedFunctionExpression expression);
		T Visit(ResolvedExternalFunctionExpression expression);
		T Visit(ResolvedFunctionCallExpression expression);
		T Visit(ResolvedExternalFunctionCallExpression expression);
		T Visit(ResolvedVarExpression expression);
		T Visit(ResolvedFieldExpression expression);
		T Visit(ResolvedConstExpression expression);
		T Visit(ResolvedTypeExpression expression);
		T Visit(ResolvedLiteralExpression expression);
		T Visit(ResolvedSymbolExpression expression);
		T Visit(ResolvedAccessExpression expression);
	}
	
	public interface IVisitor
	{
		void Visit(ResolvedFunctionExpression expression);
		void Visit(ResolvedExternalFunctionExpression expression);
		void Visit(ResolvedFunctionCallExpression expression);
		void Visit(ResolvedExternalFunctionCallExpression expression);
		void Visit(ResolvedVarExpression expression);
		void Visit(ResolvedFieldExpression expression);
		void Visit(ResolvedConstExpression expression);
		void Visit(ResolvedTypeExpression expression);
		void Visit(ResolvedLiteralExpression expression);
		void Visit(ResolvedSymbolExpression expression);
		void Visit(ResolvedAccessExpression expression);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
}

public sealed class ResolvedFunctionExpression : ResolvedExpressionNode
{
	public readonly FunctionSymbol functionSymbol;
	
	public ResolvedFunctionExpression(FunctionSymbol functionSymbol) : base(functionSymbol.EvaluatedType)
	{
		this.functionSymbol = functionSymbol;
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

public sealed class ResolvedExternalFunctionExpression : ResolvedExpressionNode
{
	public readonly ExternalFunctionSymbol functionSymbol;

	public ResolvedExternalFunctionExpression(ExternalFunctionSymbol functionSymbol) : base(
		functionSymbol.EvaluatedType)
	{
		this.functionSymbol = functionSymbol;
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

public sealed class ResolvedFunctionCallExpression : ResolvedExpressionNode
{
	public readonly FunctionSymbol functionSymbol;
	public readonly ImmutableArray<ResolvedExpressionNode> arguments;

	public ResolvedFunctionCallExpression(FunctionSymbol functionSymbol, IEnumerable<ResolvedExpressionNode> arguments)
		: base(functionSymbol.EvaluatedType)
	{
		this.functionSymbol = functionSymbol;
		this.arguments = arguments.ToImmutableArray();
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

public sealed class ResolvedExternalFunctionCallExpression : ResolvedExpressionNode
{
	public readonly ExternalFunctionSymbol functionSymbol;
	public readonly ImmutableArray<ResolvedExpressionNode> arguments;

	public ResolvedExternalFunctionCallExpression(ExternalFunctionSymbol functionSymbol,
		IEnumerable<ResolvedExpressionNode> arguments) : base(functionSymbol.EvaluatedType)
	{
		this.functionSymbol = functionSymbol;
		this.arguments = arguments.ToImmutableArray();
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

public sealed class ResolvedVarExpression : ResolvedExpressionNode
{
	public readonly VarSymbol varSymbol;
	
	public ResolvedVarExpression(VarSymbol varSymbol) : base(varSymbol.EvaluatedType)
	{
		this.varSymbol = varSymbol;
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

public sealed class ResolvedFieldExpression : ResolvedExpressionNode
{
	public readonly FieldSymbol fieldSymbol;
	
	public ResolvedFieldExpression(FieldSymbol fieldSymbol) : base(fieldSymbol.EvaluatedType)
	{
		this.fieldSymbol = fieldSymbol;
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

public sealed class ResolvedConstExpression : ResolvedExpressionNode
{
	public readonly ConstSymbol constSymbol;
	
	public ResolvedConstExpression(ConstSymbol constSymbol) : base(constSymbol.EvaluatedType)
	{
		this.constSymbol = constSymbol;
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

public sealed class ResolvedTypeExpression : ResolvedExpressionNode
{
	public readonly TypeSymbol typeSymbol;
	
	public ResolvedTypeExpression(TypeSymbol typeSymbol) : base(typeSymbol.EvaluatedType)
	{
		this.typeSymbol = typeSymbol;
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

public sealed class ResolvedLiteralExpression : ResolvedExpressionNode
{
	public readonly Token token;
	
	public ResolvedLiteralExpression(Token token, Type? type) : base(type)
	{
		this.token = token;
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

public sealed class ResolvedSymbolExpression : ResolvedExpressionNode
{
	public readonly ISymbol symbol;
	
	public ResolvedSymbolExpression(ISymbol symbol) : base(symbol.EvaluatedType)
	{
		this.symbol = symbol;
	}
	
	public ResolvedSymbolExpression(ISymbol symbol, Type? type) : base(type)
	{
		this.symbol = symbol;
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

public sealed class ResolvedAccessExpression : ResolvedExpressionNode
{
	public readonly TypeSymbol source;
	public readonly ISymbol target;
	
	public ResolvedAccessExpression(TypeSymbol source, ISymbol target) : base(target.EvaluatedType)
	{
		this.source = source;
		this.target = target;
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