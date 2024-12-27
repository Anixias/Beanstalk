using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public abstract class ResolvedExpressionNode : IResolvedAstNode
{
	public Type? Type { get; }
	public bool IsConstant { get; }

	protected ResolvedExpressionNode(Type? type, bool isConstant)
	{
		Type = type;
		IsConstant = isConstant;
	}
	public interface IVisitor<out T>
	{
		T Visit(ResolvedFunctionSymbolExpression symbolExpression);
		T Visit(ResolvedConstructorSymbolExpression symbolExpression);
		T Visit(ResolvedStringFunctionSymbolExpression symbolExpression);
		T Visit(ResolvedExternalFunctionSymbolExpression symbolExpression);
		T Visit(ResolvedFunctionCallExpression expression);
		T Visit(ResolvedConstructorCallExpression expression);
		T Visit(ResolvedStringCallExpression expression);
		T Visit(ResolvedExternalFunctionCallExpression expression);
		T Visit(ResolvedThisExpression expression);
		T Visit(ResolvedVarSymbolExpression symbolExpression);
		T Visit(ResolvedParameterSymbolExpression symbolExpression);
		T Visit(ResolvedFieldExpression expression);
		T Visit(ResolvedConstExpression expression);
		T Visit(ResolvedTypeSymbolExpression symbolExpression);
		T Visit(ResolvedImportGroupingSymbolExpression symbolExpression);
		T Visit(ResolvedLiteralExpression expression);
		T Visit(ResolvedBinaryExpression expression);
		T Visit(ResolvedSymbolExpression expression);
		T Visit(ResolvedTypeAccessExpression expression);
		T Visit(ResolvedValueAccessExpression expression);
		T Visit(ResolvedAssignmentExpression expression);
	}
	
	public interface IVisitor
	{
		void Visit(ResolvedFunctionSymbolExpression symbolExpression);
		void Visit(ResolvedConstructorSymbolExpression symbolExpression);
		void Visit(ResolvedStringFunctionSymbolExpression symbolExpression);
		void Visit(ResolvedExternalFunctionSymbolExpression symbolExpression);
		void Visit(ResolvedFunctionCallExpression expression);
		void Visit(ResolvedConstructorCallExpression expression);
		void Visit(ResolvedStringCallExpression expression);
		void Visit(ResolvedExternalFunctionCallExpression expression);
		void Visit(ResolvedThisExpression expression);
		void Visit(ResolvedVarSymbolExpression symbolExpression);
		void Visit(ResolvedParameterSymbolExpression symbolExpression);
		void Visit(ResolvedFieldExpression expression);
		void Visit(ResolvedConstExpression expression);
		void Visit(ResolvedTypeSymbolExpression symbolExpression);
		void Visit(ResolvedImportGroupingSymbolExpression symbolExpression);
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

public sealed class ResolvedFunctionSymbolExpression : ResolvedExpressionNode
{
	public readonly FunctionSymbol functionSymbol;
	
	// Todo: Pure functions vs side-effect functions
	public ResolvedFunctionSymbolExpression(FunctionSymbol functionSymbol) : base(functionSymbol.EvaluatedType, false)
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

public sealed class ResolvedConstructorSymbolExpression : ResolvedExpressionNode
{
	public readonly ConstructorSymbol constructorSymbol;
	
	public ResolvedConstructorSymbolExpression(ConstructorSymbol constructorSymbol)
		: base(constructorSymbol.EvaluatedType, false)
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

public sealed class ResolvedStringFunctionSymbolExpression : ResolvedExpressionNode
{
	public readonly StringFunctionSymbol stringFunctionSymbol;
	
	public ResolvedStringFunctionSymbolExpression(StringFunctionSymbol stringFunctionSymbol)
		: base(TypeSymbol.String.EvaluatedType, false)
	{
		this.stringFunctionSymbol = stringFunctionSymbol;
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

public sealed class ResolvedExternalFunctionSymbolExpression : ResolvedExpressionNode
{
	public readonly ExternalFunctionSymbol functionSymbol;

	public ResolvedExternalFunctionSymbolExpression(ExternalFunctionSymbol functionSymbol) : base(
		functionSymbol.EvaluatedType, false)
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
		: base(functionSymbol.ReturnType, false)
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
		IEnumerable<ResolvedExpressionNode> arguments) : base(constructorSymbol.EvaluatedType, false)
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

public sealed class ResolvedStringCallExpression : ResolvedExpressionNode
{
	public readonly StringFunctionSymbol stringFunctionSymbol;
	public readonly ResolvedExpressionNode source;

	public ResolvedStringCallExpression(StringFunctionSymbol stringFunctionSymbol, ResolvedExpressionNode source)
		: base(TypeSymbol.String.EvaluatedType, false)
	{
		this.stringFunctionSymbol = stringFunctionSymbol;
		this.source = source;
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
		IEnumerable<ResolvedExpressionNode> arguments) : base(functionSymbol.ReturnType, false)
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

public sealed class ResolvedVarSymbolExpression : ResolvedExpressionNode
{
	public readonly VarSymbol varSymbol;
	
	public ResolvedVarSymbolExpression(VarSymbol varSymbol) : base(varSymbol.EvaluatedType, false)
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

public sealed class ResolvedParameterSymbolExpression : ResolvedExpressionNode
{
	public readonly ParameterSymbol parameterSymbol;
	
	public ResolvedParameterSymbolExpression(ParameterSymbol parameterSymbol) : base(parameterSymbol.EvaluatedType, false)
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
	public ResolvedThisExpression(Type type) : base(type, false)
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
	
	public ResolvedFieldExpression(FieldSymbol fieldSymbol) : base(fieldSymbol.EvaluatedType, false)
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
	
	public ResolvedConstExpression(ConstSymbol constSymbol) : base(constSymbol.EvaluatedType, true)
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

public sealed class ResolvedTypeSymbolExpression : ResolvedExpressionNode
{
	public readonly TypeSymbol typeSymbol;
	
	// Todo: Are type symbols considered to be compile-time constants?
	public ResolvedTypeSymbolExpression(TypeSymbol typeSymbol) : base(typeSymbol.EvaluatedType, false)
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

public sealed class ResolvedImportGroupingSymbolExpression : ResolvedExpressionNode
{
	public readonly ImportGroupingSymbol importGroupingSymbol;
	
	// Todo: Are import grouping symbols considered to be compile-time constants?
	public ResolvedImportGroupingSymbolExpression(ImportGroupingSymbol importGroupingSymbol)
		: base(importGroupingSymbol.EvaluatedType, false)
	{
		this.importGroupingSymbol = importGroupingSymbol;
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
	
	public ResolvedLiteralExpression(Token token, Type? type) : base(type, true)
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
		operatorSymbol.ReturnType, left.IsConstant && right.IsConstant)
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
	
	// Todo: Are symbols compile-time constants?
	public ResolvedSymbolExpression(ISymbol symbol) : base(symbol.EvaluatedType, false)
	{
		this.symbol = symbol;
	}
	
	public ResolvedSymbolExpression(ISymbol symbol, Type? type) : base(type, false)
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
	
	public ResolvedTypeAccessExpression(Type source, ISymbol target) : base(target.EvaluatedType, target.IsConstant)
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
	
	public ResolvedValueAccessExpression(ResolvedExpressionNode source, ISymbol target)
		: base(target.EvaluatedType, target.IsConstant)
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

	public ResolvedAssignmentExpression(ResolvedExpressionNode left, ResolvedExpressionNode right)
		: base(left.Type, left.IsConstant)
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