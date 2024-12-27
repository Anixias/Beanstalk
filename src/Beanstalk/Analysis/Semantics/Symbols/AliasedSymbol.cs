namespace Beanstalk.Analysis.Semantics;

public sealed class AliasedSymbol : ISymbol
{
	public Type? EvaluatedType => LinkedSymbol.EvaluatedType;
	public bool IsConstant => LinkedSymbol.IsConstant;
	public bool IsStatic => LinkedSymbol.IsStatic;
	public ISymbol LinkedSymbol { get; }
	public string Name { get; }
	public string SymbolTypeName => "an alias";
	
	public AliasedSymbol(string name, ISymbol linkedSymbol)
	{
		Name = name;
		LinkedSymbol = linkedSymbol;
	}
}