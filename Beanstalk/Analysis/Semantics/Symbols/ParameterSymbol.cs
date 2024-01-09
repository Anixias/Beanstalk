using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public sealed class ParameterSymbol : ISymbol
{
	public string SymbolTypeName => "a parameter";
	public string Name => VarSymbol.Name;
	public bool IsConstant => false;
	public bool IsStatic => false;
	public Type? EvaluatedType => VarSymbol.EvaluatedType;
	public VarSymbol VarSymbol { get; }
	public ExpressionNode? Expression { get; }
	public bool IsVariadic { get; }
	public uint Index { get; }
	
	public ParameterSymbol(VarSymbol varSymbol, ExpressionNode? expression, bool isVariadic, uint index)
	{
		VarSymbol = varSymbol;
		Expression = expression;
		IsVariadic = isVariadic;
		Index = index;
	}
}