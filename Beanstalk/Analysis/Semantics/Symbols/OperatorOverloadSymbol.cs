using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public abstract class OperatorOverloadSymbol : ISymbol
{
	public string SymbolTypeName => "an operator overload";
	public string Name { get; }
	public Type ReturnType { get; }
	public Scope Body { get; }

	protected OperatorOverloadSymbol(string name, Type returnType, Scope body)
	{
		Name = name;
		Body = body;
		ReturnType = returnType;
	}
}

public sealed class BinaryOperatorOverloadSymbol : OperatorOverloadSymbol
{
	public ParameterSymbol Left { get; }
	public BinaryExpression.Operation Operation { get; }
	public ParameterSymbol Right { get; }

	public BinaryOperatorOverloadSymbol(ParameterSymbol left, BinaryExpression.Operation operation, 
		ParameterSymbol right, Type returnType, Scope body)
		: base($"$operator({left.VarSymbol.Type}[{operation}]{right.VarSymbol.Type}:>{returnType})", returnType, body)
	{
		Left = left;
		Operation = operation;
		Right = right;
	}
}

public sealed class UnaryOperatorOverloadSymbol : OperatorOverloadSymbol
{
	public ParameterSymbol Operand { get; }
	public UnaryExpression.Operation Operation { get; }
	public bool IsPrefix { get; }

	public UnaryOperatorOverloadSymbol(ParameterSymbol operand, UnaryExpression.Operation operation,
		bool isPrefix, Type returnType, Scope body)
		: base(isPrefix
			? $"$operator([{operation}]{operand.VarSymbol.Type}:>{returnType})"
			: $"$operator({operand.VarSymbol.Type}[{operation}]:>{returnType})",
			returnType, body)
	{
		Operand = operand;
		Operation = operation;
		IsPrefix = isPrefix;
	}
}