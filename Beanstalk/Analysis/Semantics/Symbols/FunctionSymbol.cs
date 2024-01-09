using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public sealed class FunctionSymbol : IFunctionSymbol
{
	// Todo: Support instance methods
	public bool IsStatic => true;
	public string SymbolTypeName => "a function";
	public bool IsConstant => false;
	public string Name { get; }
	public ImmutableArray<TypeParameterSymbol> TypeParameters { get; set; }
	public ImmutableArray<ParameterSymbol> Parameters { get; set; }
	public IBuffer Source { get; }
	public TextRange SignatureRange { get; }
	public Type? ReturnType { get; set; }
	public Scope Body { get; }
	public List<FunctionSymbol> Overloads { get; } = [];
	public Type? EvaluatedType => GetFunctionType();

	public FunctionSymbol(string name, Scope body, IBuffer source, TextRange signatureRange)
	{
		Name = name;
		Body = body;
		SignatureRange = signatureRange;
		Source = source;
	}

	public bool SignatureMatches(FunctionSymbol functionSymbol)
	{
		if (Name != functionSymbol.Name)
			return false;

		if (Parameters.Length != functionSymbol.Parameters.Length)
			return false;

		for (var i = 0; i < Parameters.Length; i++)
		{
			var otherParameterType = functionSymbol.Parameters[i].VarSymbol.EvaluatedType;
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