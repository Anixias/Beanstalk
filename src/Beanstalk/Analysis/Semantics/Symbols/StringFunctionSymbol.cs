namespace Beanstalk.Analysis.Semantics;

public sealed class StringFunctionSymbol : IFunctionSymbol
{
	public bool IsStatic => false;
	public bool IsConstant => false;
	public const string InternalName = "string()";
	public string SymbolTypeName => "a string function";
	public Type EvaluatedType => TypeSymbol.String.EvaluatedType;
	public TypeSymbol Owner { get; }
	public string Name => InternalName;
	public ParameterSymbol This { get; }
	public Scope Body { get; }
	
	public StringFunctionSymbol(TypeSymbol owner, ParameterSymbol @this, Scope body)
	{
		Owner = owner;
		This = @this;
		Body = body;
	}
}