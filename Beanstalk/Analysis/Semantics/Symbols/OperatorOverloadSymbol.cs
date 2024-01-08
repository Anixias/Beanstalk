using System.Collections;
using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public abstract class OperatorOverloadSymbol : IFunctionSymbol
{
	public bool IsNative { get; }
	public bool IsStatic => true;
	public string SymbolTypeName => "an operator overload";
	public Type EvaluatedType => ReturnType;
	public string Name { get; }
	public Type ReturnType { get; }
	public Scope Body { get; }

	protected OperatorOverloadSymbol(string name, Type returnType, Scope body, bool isNative)
	{
		Name = name;
		Body = body;
		IsNative = isNative;
		ReturnType = returnType;
	}
}

public sealed class BinaryOperatorOverloadSymbol : OperatorOverloadSymbol
{
	public ParameterSymbol Left { get; }
	public BinaryExpression.Operation Operation { get; }
	public ParameterSymbol Right { get; }

	public BinaryOperatorOverloadSymbol(ParameterSymbol left, BinaryExpression.Operation operation,
		ParameterSymbol right, Type returnType, Scope body, bool isNative) : base(
		GenerateName(left.VarSymbol.EvaluatedType!, operation, right.VarSymbol.EvaluatedType!, returnType), returnType,
		body, isNative)
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

	public UnaryOperatorOverloadSymbol(ParameterSymbol operand, UnaryExpression.Operation operation, Type returnType,
		Scope body, bool isNative) : base(GenerateName(operand.VarSymbol.EvaluatedType!, operation, returnType),
		returnType, body, isNative)
	{
		Operand = operand;
		Operation = operation;
	}

	public static string GenerateName(Type operandType, UnaryExpression.Operation operation, Type returnType)
	{
		return $"$operator([{operation}]{operandType}{TokenType.OpReturnType}{returnType})";
	}
}