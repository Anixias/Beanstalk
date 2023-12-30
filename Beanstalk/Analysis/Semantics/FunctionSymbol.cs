﻿using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public sealed class FunctionSymbol : ISymbol
{
	public string SymbolTypeName => "a function";
	public string Name { get; }
	public ImmutableArray<TypeParameterSymbol> TypeParameters { get; }
	public ImmutableArray<ParameterSymbol> Parameters { get; }
	public Type? ReturnType { get; set; }
	public Scope Body { get; }

	public FunctionSymbol(string name, IEnumerable<TypeParameterSymbol> typeParameters,
		IEnumerable<ParameterSymbol> parameters, Scope body)
	{
		Name = name;
		TypeParameters = typeParameters.ToImmutableArray();
		Parameters = parameters.ToImmutableArray();
		Body = body;
	}
}