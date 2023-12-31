namespace Beanstalk.Analysis.Semantics;

public sealed class CastOverloadSymbol : ISymbol
{
	public string SymbolTypeName => "a cast overload";
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

		Name = $"${(isImplicit ? "i" : "e")}cast({parameter.VarSymbol.Type}::{returnType})";
	}
}