using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class ExpressionNode : IAstNode
{
	public readonly TextRange range;

	protected ExpressionNode(TextRange range)
	{
		this.range = range;
	}
}

public sealed class TokenExpression : ExpressionNode
{
	public readonly Token token;

	public TokenExpression(Token token) : base(token.Range)
	{
		this.token = token;
	}

	public override string ToString()
	{
		return token.Text;
	}
}

public sealed class TupleExpression : ExpressionNode
{
	public readonly ImmutableArray<ExpressionNode> expressions;

	public TupleExpression(IEnumerable<ExpressionNode> expressions, TextRange range) : base(range)
	{
		this.expressions = expressions.ToImmutableArray();
	}
}

public sealed class ListExpression : ExpressionNode
{
	public readonly ImmutableArray<ExpressionNode> expressions;
	public readonly Type? type;

	public ListExpression(IEnumerable<ExpressionNode> expressions, Type? type, TextRange range) : base(range)
	{
		this.type = type;
		this.expressions = expressions.ToImmutableArray();
	}
}

public sealed class MapExpression : ExpressionNode
{
	public readonly ImmutableArray<(ExpressionNode, ExpressionNode)> keyValuePairs;
	public readonly TupleType? type;

	public MapExpression(IEnumerable<(ExpressionNode, ExpressionNode)> keyValuePairs, TupleType? type, TextRange range)
		: base(range)
	{
		this.type = type;
		this.keyValuePairs = keyValuePairs.ToImmutableArray();
	}
}

public sealed class InstantiationExpression : ExpressionNode
{
	public readonly Type type;
	public readonly ImmutableDictionary<Token, ExpressionNode> values;

	public InstantiationExpression(Type type, IDictionary<Token, ExpressionNode> values, TextRange range) : base(range)
	{
		this.type = type;
		this.values = values.ToImmutableDictionary();
	}
}

public sealed class FunctionCallExpression : ExpressionNode
{
	public readonly ExpressionNode caller;
	public readonly ImmutableArray<ExpressionNode> arguments;

	public FunctionCallExpression(ExpressionNode caller, IEnumerable<ExpressionNode> arguments, TextRange range)
		: base(range)
	{
		this.caller = caller;
		this.arguments = arguments.ToImmutableArray();
	}
}

public sealed class CastExpression : ExpressionNode
{
	public readonly ExpressionNode source;
	public readonly Type targetType;

	public CastExpression(ExpressionNode source, Type targetType, TextRange range) : base(range)
	{
		this.source = source;
		this.targetType = targetType;
	}
}

public sealed class AccessExpression : ExpressionNode
{
	public readonly ExpressionNode source;
	public readonly ExpressionNode target;
	public readonly bool nullCheck;

	public AccessExpression(ExpressionNode source, ExpressionNode target, bool nullCheck, TextRange range) : base(range)
	{
		this.source = source;
		this.target = target;
		this.nullCheck = nullCheck;
	}
}

public sealed class IndexExpression : ExpressionNode
{
	public readonly ExpressionNode source;
	public readonly ExpressionNode index;
	public readonly bool nullCheck;

	public IndexExpression(ExpressionNode source, ExpressionNode index, bool nullCheck, TextRange range) : base(range)
	{
		this.source = source;
		this.index = index;
		this.nullCheck = nullCheck;
	}
}

public sealed class AssignmentExpression : ExpressionNode
{
	public readonly ExpressionNode target;
	public readonly ExpressionNode expression;
	
	public AssignmentExpression(ExpressionNode target, ExpressionNode expression, TextRange range) : base(range)
	{
		this.target = target;
		this.expression = expression;
	}
}

public sealed class LambdaExpression : ExpressionNode
{
	public readonly ImmutableArray<Parameter> parameters;
	public readonly Type? returnType;
	public readonly StatementNode body;

	public LambdaExpression(IEnumerable<Parameter> parameters, Type? returnType, StatementNode body, TextRange range) 
		: base(range)
	{
		this.parameters = parameters.ToImmutableArray();
		this.returnType = returnType;
		this.body = body;
	}
}

public sealed class ConditionalExpression : ExpressionNode
{
	public readonly ExpressionNode condition;
	public readonly ExpressionNode trueExpression;
	public readonly ExpressionNode? falseExpression;

	public ConditionalExpression(ExpressionNode condition, ExpressionNode trueExpression,
		ExpressionNode? falseExpression, TextRange range) : base(range)
	{
		this.condition = condition;
		this.trueExpression = trueExpression;
		this.falseExpression = falseExpression;
	}
}

public sealed class BinaryExpression : ExpressionNode
{
	public enum Operation
	{
		NullCoalescence,
		LogicalOr,
		LogicalXor,
		LogicalAnd,
		Equals,
		NotEquals,
		BitwiseOr,
		BitwiseXor,
		BitwiseAnd,
		LessThan,
		GreaterThan,
		LessEqual,
		GreaterEqual,
		Is,
		As,
		RotLeft,
		RotRight,
		ShiftLeft,
		ShiftRight,
		Add,
		Subtract,
		Multiply,
		Divide,
		PosMod,
		Modulo,
		Power,
		RangeInclusive,
		RangeExclusive
	}
	
	public readonly ExpressionNode left;
	public readonly Operation operation;
	public readonly ExpressionNode right;

	public BinaryExpression(ExpressionNode left, Operation operation, ExpressionNode right, TextRange range)
		: base(range)
	{
		this.left = left;
		this.operation = operation;
		this.right = right;
	}
}

public sealed class UnaryExpression : ExpressionNode
{
	public enum Operation
	{
		PreIncrement,
		PreDecrement,
		PostIncrement,
		PostDecrement,
		Identity,
		Negate,
		BitwiseNegate,
		LogicalNot,
		Await
	}
	
	public readonly ExpressionNode operand;
	public readonly Operation operation;
	public readonly bool isPrefix;

	public UnaryExpression(ExpressionNode operand, Operation operation, bool isPrefix, TextRange range) : base(range)
	{
		this.operand = operand;
		this.operation = operation;
		this.isPrefix = isPrefix;
	}
}

public sealed class SwitchExpression : ExpressionNode
{
	// Todo
	public SwitchExpression(TextRange range) : base(range)
	{
	}
}

public sealed class WithExpression : ExpressionNode
{
	// Todo
	public WithExpression(TextRange range) : base(range)
	{
	}
}