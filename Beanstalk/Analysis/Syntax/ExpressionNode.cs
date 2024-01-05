using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class ExpressionNode : IAstNode
{
	public interface IVisitor<out T>
	{
		T Visit(TokenExpression expression);
		T Visit(TupleExpression expression);
		T Visit(ListExpression expression);
		T Visit(MapExpression expression);
		T Visit(InstantiationExpression expression);
		T Visit(FunctionCallExpression expression);
		T Visit(CastExpression expression);
		T Visit(AccessExpression expression);
		T Visit(IndexExpression expression);
		T Visit(AssignmentExpression expression);
		T Visit(LambdaExpression expression);
		T Visit(ConditionalExpression expression);
		T Visit(BinaryExpression expression);
		T Visit(UnaryExpression expression);
		T Visit(SwitchExpression expression);
		T Visit(WithExpression expression);
		T Visit(BinaryOperationExpression expression);
		T Visit(UnaryOperationExpression expression);
		T Visit(PrimaryOperationExpression expression);
		T Visit(InterpolatedStringExpression expression);
	}
	
	public interface IVisitor
	{
		void Visit(TokenExpression expression);
		void Visit(TupleExpression expression);
		void Visit(ListExpression expression);
		void Visit(MapExpression expression);
		void Visit(InstantiationExpression expression);
		void Visit(FunctionCallExpression expression);
		void Visit(CastExpression expression);
		void Visit(AccessExpression expression);
		void Visit(IndexExpression expression);
		void Visit(AssignmentExpression expression);
		void Visit(LambdaExpression expression);
		void Visit(ConditionalExpression expression);
		void Visit(BinaryExpression expression);
		void Visit(UnaryExpression expression);
		void Visit(SwitchExpression expression);
		void Visit(WithExpression expression);
		void Visit(BinaryOperationExpression expression);
		void Visit(UnaryOperationExpression expression);
		void Visit(PrimaryOperationExpression expression);
		void Visit(InterpolatedStringExpression expression);
	}
	
	public readonly TextRange range;

	protected ExpressionNode(TextRange range)
	{
		this.range = range;
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class TupleExpression : ExpressionNode
{
	public readonly ImmutableArray<ExpressionNode> expressions;

	public TupleExpression(IEnumerable<ExpressionNode> expressions, TextRange range) : base(range)
	{
		this.expressions = expressions.ToImmutableArray();
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

public sealed class ListExpression : ExpressionNode
{
	public readonly ImmutableArray<ExpressionNode> expressions;
	public readonly SyntaxType? type;

	public ListExpression(IEnumerable<ExpressionNode> expressions, SyntaxType? type, TextRange range) : base(range)
	{
		this.type = type;
		this.expressions = expressions.ToImmutableArray();
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

public sealed class MapExpression : ExpressionNode
{
	public readonly ImmutableArray<KeyValuePair<ExpressionNode, ExpressionNode>> keyValuePairs;
	public readonly TupleSyntaxType? type;

	public MapExpression(IEnumerable<KeyValuePair<ExpressionNode, ExpressionNode>> keyValuePairs, TupleSyntaxType? type,
		TextRange range) : base(range)
	{
		this.type = type;
		this.keyValuePairs = keyValuePairs.ToImmutableArray();
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

public sealed class InstantiationExpression : ExpressionNode
{
	public readonly SyntaxType syntaxType;
	public readonly ImmutableDictionary<Token, ExpressionNode> values;

	public InstantiationExpression(SyntaxType syntaxType, IDictionary<Token, ExpressionNode> values, TextRange range) : base(range)
	{
		this.syntaxType = syntaxType;
		this.values = values.ToImmutableDictionary();
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class CastExpression : ExpressionNode
{
	public readonly ExpressionNode source;
	public readonly SyntaxType targetSyntaxType;

	public CastExpression(ExpressionNode source, SyntaxType targetSyntaxType, TextRange range) : base(range)
	{
		this.source = source;
		this.targetSyntaxType = targetSyntaxType;
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

public sealed class AccessExpression : ExpressionNode
{
	public readonly ExpressionNode source;
	public readonly Token target;
	public readonly bool nullCheck;

	public AccessExpression(ExpressionNode source, Token target, bool nullCheck, TextRange range) : base(range)
	{
		this.source = source;
		this.target = target;
		this.nullCheck = nullCheck;
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class AssignmentExpression : ExpressionNode
{
	public readonly ExpressionNode left;
	public readonly ExpressionNode right;
	
	public AssignmentExpression(ExpressionNode left, ExpressionNode right, TextRange range) : base(range)
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

public sealed class LambdaExpression : ExpressionNode
{
	public readonly ImmutableArray<Parameter> parameters;
	public readonly SyntaxType? returnType;
	public readonly StatementNode body;

	public LambdaExpression(IEnumerable<Parameter> parameters, SyntaxType? returnType, StatementNode body, TextRange range) 
		: base(range)
	{
		this.parameters = parameters.ToImmutableArray();
		this.returnType = returnType;
		this.body = body;
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class BinaryExpression : ExpressionNode
{
	public enum Operation
	{
		NullCoalescence,
		Equals,
		NotEquals,
		Or,
		Xor,
		And,
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class SwitchExpression : ExpressionNode
{
	// Todo
	public SwitchExpression(TextRange range) : base(range)
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

public sealed class WithExpression : ExpressionNode
{
	// Todo
	public WithExpression(TextRange range) : base(range)
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

public abstract class OperationExpression : ExpressionNode
{
	public readonly Token? op;
	
	protected OperationExpression(Token? op, TextRange range) : base(range)
	{
		this.op = op;
	}
}

public sealed class BinaryOperationExpression : OperationExpression
{
	public readonly Parameter left;
	public readonly BinaryExpression.Operation operation;
	public readonly Parameter right;

	public BinaryOperationExpression(Parameter left, BinaryExpression.Operation operation, Token op, Parameter right,
		TextRange range) : base(op, range)
	{
		this.left = left;
		this.operation = operation;
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

public sealed class UnaryOperationExpression : OperationExpression
{
	public UnaryExpression.Operation operation;
	public readonly Parameter operand;
	public readonly bool isPrefix;

	public UnaryOperationExpression(UnaryExpression.Operation operation, Token op, Parameter operand, bool isPrefix,
		TextRange range) : base(op, range)
	{
		this.operation = operation;
		this.operand = operand;
		this.isPrefix = isPrefix;
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

public sealed class PrimaryOperationExpression : OperationExpression
{
	public readonly Parameter operand;

	public PrimaryOperationExpression(Parameter operand) : base(null, operand.range)
	{
		this.operand = operand;
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

public sealed class InterpolatedStringExpression : ExpressionNode
{
	public readonly ImmutableArray<ExpressionNode> parts;
	
	public InterpolatedStringExpression(IEnumerable<ExpressionNode> parts, TextRange range) : base(range)
	{
		this.parts = parts.ToImmutableArray();
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