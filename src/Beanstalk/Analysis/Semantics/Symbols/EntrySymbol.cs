using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public sealed class EntrySymbol : IFunctionSymbol
{
	public bool IsStatic => true;
	public string SymbolTypeName => "an entry point";
	public string Name => "$entry";
	public bool IsConstant => false;
	public ImmutableArray<ParameterSymbol> Parameters { get; }
	public Scope Body { get; }
	public Type? EvaluatedType => null;

	public EntrySymbol(IEnumerable<ParameterSymbol> parameters, Scope body)
	{
		Parameters = parameters.ToImmutableArray();
		Body = body;
	}
}