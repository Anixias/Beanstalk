using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public sealed class EntrySymbol : ISymbol
{
	public string SymbolTypeName => "an entry point";
	public string Name => "$entry";
	public ImmutableArray<ParameterSymbol> Parameters { get; }
	public Scope Body { get; }
	public Type? EvaluatedType => null;

	public EntrySymbol(IEnumerable<ParameterSymbol> parameters, Scope body)
	{
		Parameters = parameters.ToImmutableArray();
		Body = body;
	}
}