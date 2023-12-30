using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public class FunctionSymbol : ISymbol
{
	public string SymbolTypeName => "a function";
	public string Name { get; }
	public ImmutableArray<VarSymbol> Parameters { get; }
	public Type? ReturnType { get; set; }
	
	public FunctionSymbol(string name, IEnumerable<VarSymbol> parameters)
	{
		Name = name;
		Parameters = parameters.ToImmutableArray();
	}
}