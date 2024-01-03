namespace Beanstalk.Analysis.Semantics;

public sealed class CastOverloadSymbol : ISymbol
{
	public string SymbolTypeName => "a cast overload";
	public Type EvaluatedType => ReturnType;
	public string Name { get; }
	public bool IsImplicit { get; }
	public ParameterSymbol Parameter { get; }
	public Type ReturnType { get; set; }
	public Scope Body { get; }
	
	public CastOverloadSymbol(bool isImplicit, ParameterSymbol parameter, Type returnType, Scope body)
	{
		IsImplicit = isImplicit;
		Parameter = parameter;
		ReturnType = returnType;
		Body = body;

		Name = GenerateName(isImplicit, parameter.VarSymbol.EvaluatedType!, returnType);
	}

	public static string GenerateName(bool isImplicit, Type parameterType, Type returnType)
	{
		return $"${(isImplicit ? "i" : "e")}cast({parameterType}::{returnType})";
	}
}