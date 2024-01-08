using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;
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
		T Visit(ResolvedConstructorExpression expression);
		T Visit(ResolvedExternalFunctionExpression expression);
		T Visit(ResolvedFunctionCallExpression expression);
		T Visit(ResolvedConstructorCallExpression expression);
		T Visit(ResolvedExternalFunctionCallExpression expression);
		T Visit(ResolvedThisExpression expression);
		T Visit(ResolvedVarExpression expression);
		T Visit(ResolvedParameterExpression expression);
		T Visit(ResolvedFieldExpression expression);
		T Visit(ResolvedConstExpression expression);
		T Visit(ResolvedTypeExpression expression);
		T Visit(ResolvedLiteralExpression expression);
		T Visit(ResolvedBinaryExpression expression);
		T Visit(ResolvedSymbolExpression expression);
		T Visit(ResolvedTypeAccessExpression expression);
		T Visit(ResolvedValueAccessExpression expression);
		T Visit(ResolvedAssignmentExpression expression);
	}
	
	public interface IVisitor
	{
		void Visit(ResolvedFunctionExpression expression);
		void Visit(ResolvedConstructorExpression expression);
		void Visit(ResolvedExternalFunctionExpression expression);
		void Visit(ResolvedFunctionCallExpression expression);
		void Visit(ResolvedConstructorCallExpression expression);
		void Visit(ResolvedExternalFunctionCallExpression expression);
		void Visit(ResolvedThisExpression expression);
		void Visit(ResolvedVarExpression expression);
		void Visit(ResolvedParameterExpression expression);
		void Visit(ResolvedFieldExpression expression);
		void Visit(ResolvedConstExpression expression);
		void Visit(ResolvedTypeExpression expression);
		void Visit(ResolvedLiteralExpression expression);
		void Visit(ResolvedBinaryExpression expression);
		void Visit(ResolvedSymbolExpression expression);
		void Visit(ResolvedTypeAccessExpression expression);
		void Visit(ResolvedValueAccessExpression expression);
		void Visit(ResolvedAssignmentExpression expression);
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

public sealed class ResolvedConstructorExpression : ResolvedExpressionNode
{
	public readonly ConstructorSymbol constructorSymbol;
	
	public ResolvedConstructorExpression(ConstructorSymbol constructorSymbol) : base(constructorSymbol.EvaluatedType)
	{
		this.constructorSymbol = constructorSymbol;
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

public sealed class ResolvedConstructorCallExpression : ResolvedExpressionNode
{
	public readonly ConstructorSymbol constructorSymbol;
	public readonly ImmutableArray<ResolvedExpressionNode> arguments;

	public ResolvedConstructorCallExpression(ConstructorSymbol constructorSymbol,
		IEnumerable<ResolvedExpressionNode> arguments) : base(constructorSymbol.EvaluatedType)
	{
		this.constructorSymbol = constructorSymbol;
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

public sealed class ResolvedParameterExpression : ResolvedExpressionNode
{
	public readonly ParameterSymbol parameterSymbol;
	
	public ResolvedParameterExpression(ParameterSymbol parameterSymbol) : base(parameterSymbol.EvaluatedType)
	{
		this.parameterSymbol = parameterSymbol;
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

public sealed class ResolvedThisExpression : ResolvedExpressionNode
{
	public ResolvedThisExpression(Type type) : base(type)
	{
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

public sealed class ResolvedBinaryExpression : ResolvedExpressionNode
{
	public readonly ResolvedExpressionNode left;
	public readonly ResolvedExpressionNode right;
	public readonly BinaryExpression.Operation operation;
	public readonly OperatorOverloadSymbol operatorSymbol;

	public ResolvedBinaryExpression(ResolvedExpressionNode left, ResolvedExpressionNode right,
		OperatorOverloadSymbol operatorSymbol, BinaryExpression.Operation operation) : base(
		operatorSymbol.ReturnType)
	{
		this.left = left;
		this.right = right;
		this.operatorSymbol = operatorSymbol;
		this.operation = operation;
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

public sealed class ResolvedTypeAccessExpression : ResolvedExpressionNode
{
	public readonly Type source;
	public readonly ISymbol target;
	
	public ResolvedTypeAccessExpression(Type source, ISymbol target) : base(target.EvaluatedType)
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

public sealed class ResolvedValueAccessExpression : ResolvedExpressionNode
{
	public readonly ResolvedExpressionNode source;
	public readonly ISymbol target;
	
	public ResolvedValueAccessExpression(ResolvedExpressionNode source, ISymbol target) : base(target.EvaluatedType)
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

public sealed class ResolvedAssignmentExpression : ResolvedExpressionNode
{
	public readonly ResolvedExpressionNode left;
	public readonly ResolvedExpressionNode right;

	public ResolvedAssignmentExpression(ResolvedExpressionNode left, ResolvedExpressionNode right) : base(
		left.Type)
	{
		this.left = left;
		this.right = right;
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