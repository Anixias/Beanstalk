using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public sealed class ParameterSymbol : ISymbol
{
	public string SymbolTypeName => "a parameter";
	public string Name => VarSymbol.Name;
	public Type? EvaluatedType => VarSymbol.EvaluatedType;
	public VarSymbol VarSymbol { get; }
	public ExpressionNode? Expression { get; }
	public bool IsVariadic { get; }
	
	public ParameterSymbol(VarSymbol varSymbol, ExpressionNode? expression, bool isVariadic)
	{
		VarSymbol = varSymbol;
		Expression = expression;
		IsVariadic = isVariadic;
	}
}