using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Beanstalk.Analysis.Semantics;

public sealed class Scope : IEnumerable<Scope>
{
	public Scope? Parent { get; }
	public SymbolTable SymbolTable { get; } = new();
	private readonly List<Scope> childScopes = [];
	
	public Scope(Scope? parent = null)
	{
		Parent = parent;
		parent?.AddChildScope(this);
	}

	public void AddSymbol(ISymbol symbol)
	{
		SymbolTable.Add(symbol);
	}

	public ISymbol? LookupSymbol(string name)
	{
		if (SymbolTable.Lookup(name) is { } symbol)
			return symbol;

		return Parent?.LookupSymbol(name);
	}

	/// <summary>
	/// Searches recursively for a symbol matching the given name and type
	/// </summary>
	/// <param name="name">The name of the symbol to search for</param>
	/// <param name="symbol">Contains <see langword="null"/> if the symbol does not exist or is not of the given type;
	/// else, contains the symbol</param>
	/// <param name="existingSymbol">The symbol found by name; it may not be of the desired type</param>
	/// <typeparam name="T">The symbol type to filter for</typeparam>
	/// <returns>
	/// <see langword="true"/> if the symbol exists of the given type; <see langword="false"/> otherwise
	/// </returns>
	public bool LookupSymbol<T>(string name, out T? symbol, [NotNullWhen(true)] out ISymbol? existingSymbol)
		where T : class
	{
		symbol = null;
		existingSymbol = null;

		if (SymbolTable.Lookup(name) is not { } genericSymbol)
			return Parent?.LookupSymbol(name, out symbol, out existingSymbol) ?? false;

		existingSymbol = genericSymbol;
		if (genericSymbol is not T typedSymbol)
			return false;
			
		symbol = typedSymbol;
		return true;

	}

	public IEnumerator<Scope> GetEnumerator()
	{
		return childScopes.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void AddChildScope(Scope scope)
	{
		childScopes.Add(scope);
	}
}