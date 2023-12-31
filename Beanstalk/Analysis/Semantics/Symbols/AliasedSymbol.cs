namespace Beanstalk.Analysis.Semantics;

public sealed class AliasedSymbol : ISymbol
{
	public ISymbol LinkedSymbol { get; }
	public string Name { get; }
	public string SymbolTypeName => "an alias";
	
	public AliasedSymbol(string name, ISymbol linkedSymbol)
	{
		Name = name;
		LinkedSymbol = linkedSymbol;
	}
}