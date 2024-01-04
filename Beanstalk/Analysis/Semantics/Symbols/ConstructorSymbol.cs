using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public sealed class ConstructorSymbol : ISymbol
{
	public string SymbolTypeName => "a constructor";
	public Type? EvaluatedType { get; }
	public string Name { get; }
	public ParameterSymbol This { get; }
	public ImmutableArray<ParameterSymbol> Parameters { get; }
	public Scope Body { get; }
	public List<ConstructorSymbol> Overloads { get; } = [];

	public ConstructorSymbol(TypeSymbol owner, ParameterSymbol @this, IEnumerable<ParameterSymbol> parameters,
		Scope body)
	{
		Name = "$constructor";
		This = @this;
		Parameters = parameters.ToImmutableArray();
		Body = body;
		EvaluatedType = new BaseType(owner);
	}

	public bool SignatureMatches(ConstructorSymbol constructorSymbol)
	{
		if (Parameters.Length != constructorSymbol.Parameters.Length)
			return false;

		for (var i = 0; i < Parameters.Length; i++)
		{
			var otherParameterType = constructorSymbol.Parameters[i].VarSymbol.EvaluatedType;
			if (Parameters[i].VarSymbol.EvaluatedType is not { } parameterType)
			{
				if (otherParameterType is not null)
					return false;

				continue;
			}
			
			if (!parameterType.Equals(otherParameterType))
				return false;
		}

		return true;
	}
}