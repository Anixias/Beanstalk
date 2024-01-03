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
}

public sealed class ResolvedFunctionExpression : ResolvedExpressionNode
{
	public readonly FunctionSymbol functionSymbol;
	
	public ResolvedFunctionExpression(FunctionSymbol functionSymbol) : base(functionSymbol.EvaluatedType)
	{
		this.functionSymbol = functionSymbol;
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
}

public sealed class ResolvedVarExpression : ResolvedExpressionNode
{
	public readonly VarSymbol varSymbol;
	
	public ResolvedVarExpression(VarSymbol varSymbol) : base(varSymbol.EvaluatedType)
	{
		this.varSymbol = varSymbol;
	}
}

public sealed class ResolvedFieldExpression : ResolvedExpressionNode
{
	public readonly FieldSymbol fieldSymbol;
	
	public ResolvedFieldExpression(FieldSymbol fieldSymbol) : base(fieldSymbol.EvaluatedType)
	{
		this.fieldSymbol = fieldSymbol;
	}
}

public sealed class ResolvedConstExpression : ResolvedExpressionNode
{
	public readonly ConstSymbol constSymbol;
	
	public ResolvedConstExpression(ConstSymbol constSymbol) : base(constSymbol.EvaluatedType)
	{
		this.constSymbol = constSymbol;
	}
}

public sealed class ResolvedTypeExpression : ResolvedExpressionNode
{
	public readonly TypeSymbol typeSymbol;
	
	public ResolvedTypeExpression(TypeSymbol typeSymbol) : base(typeSymbol.EvaluatedType)
	{
		this.typeSymbol = typeSymbol;
	}
}

public sealed class ResolvedLiteralExpression : ResolvedExpressionNode
{
	public readonly Token token;
	
	public ResolvedLiteralExpression(Token token, Type? type) : base(type)
	{
		this.token = token;
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
}