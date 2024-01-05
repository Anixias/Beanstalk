using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public abstract class OperatorOverloadSymbol : IFunctionSymbol
{
	public bool IsStatic => true;
	public string SymbolTypeName => "an operator overload";
	public Type EvaluatedType => ReturnType;
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
		: base(GenerateName(left.VarSymbol.EvaluatedType!, operation, right.VarSymbol.EvaluatedType!, returnType), returnType, body)
	{
		Left = left;
		Operation = operation;
		Right = right;
	}

	public static string GenerateName(Type leftType, BinaryExpression.Operation operation, Type rightType,
		Type returnType)
	{
		return $"$operator({leftType}[{operation}]{rightType}:>{returnType})";
	}
}

public sealed class UnaryOperatorOverloadSymbol : OperatorOverloadSymbol
{
	public ParameterSymbol Operand { get; }
	public UnaryExpression.Operation Operation { get; }
	public bool IsPrefix { get; }

	public UnaryOperatorOverloadSymbol(ParameterSymbol operand, UnaryExpression.Operation operation,
		bool isPrefix, Type returnType, Scope body)
		: base(GenerateName(isPrefix, operand.VarSymbol.EvaluatedType!, operation, returnType),
			returnType, body)
	{
		Operand = operand;
		Operation = operation;
		IsPrefix = isPrefix;
	}

	public static string GenerateName(bool isPrefix, Type operandType, UnaryExpression.Operation operation,
		Type returnType)
	{
		return isPrefix
			? $"$operator([{operation}]{operandType}:>{returnType})"
			: $"$operator({operandType}[{operation}]:>{returnType})";
	}
}