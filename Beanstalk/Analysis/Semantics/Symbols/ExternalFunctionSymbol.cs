using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public class ExternalFunctionSymbol : ISymbol
{
	public string SymbolTypeName => "an external function";
	public bool IsConstant => false;
	public bool IsStatic => false;
	public string Name { get; }
	public ImmutableArray<ParameterSymbol> Parameters { get; }
	public Type? ReturnType { get; init; }
	public Type? EvaluatedType => GetFunctionType();
	public IReadOnlyDictionary<string, string> Attributes { get; }

	public ExternalFunctionSymbol(string name, IEnumerable<ParameterSymbol> parameters,
		IReadOnlyDictionary<string, string> attributes)
	{
		Name = name;
		Parameters = parameters.ToImmutableArray();
		Attributes = attributes;
	}

	public bool SignatureMatches(ExternalFunctionSymbol externalFunctionSymbol)
	{
		if (Name != externalFunctionSymbol.Name)
			return false;

		if (Parameters.Length != externalFunctionSymbol.Parameters.Length)
			return false;

		for (var i = 0; i < Parameters.Length; i++)
		{
			var otherParameterType = externalFunctionSymbol.Parameters[i].VarSymbol.EvaluatedType;
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

	public FunctionType? GetFunctionType()
	{
		var parameterTypes = new List<Type>();
		foreach (var parameter in Parameters)
		{
			if (parameter.VarSymbol.EvaluatedType is not { } type)
				return null;
			
			parameterTypes.Add(type);
		}
		
		return new FunctionType(parameterTypes, ReturnType);
	}
}