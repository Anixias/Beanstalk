using System.Collections;

namespace Beanstalk.Analysis.Semantics;

public sealed class SymbolTable : IEnumerable<KeyValuePair<string, ISymbol>>
{
	public Dictionary<string, ISymbol>.KeyCollection Keys => symbols.Keys;
	public Dictionary<string, ISymbol>.ValueCollection Values => symbols.Values;
	private readonly Dictionary<string, ISymbol> symbols = new();

	public bool TryAdd(ISymbol symbol)
	{
		return symbols.TryAdd(symbol.Name, symbol);
	}

	public void Add(ISymbol symbol)
	{
		symbols.Add(symbol.Name, symbol);
	}

	public bool Contains(string name)
	{
		return symbols.ContainsKey(name);
	}

	public ISymbol? Lookup(string name)
	{
		return symbols.GetValueOrDefault(name);
	}

	public IEnumerator<KeyValuePair<string, ISymbol>> GetEnumerator()
	{
		return symbols.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		symbols.Clear();
	}
}