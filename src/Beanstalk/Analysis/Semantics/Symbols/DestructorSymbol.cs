namespace Beanstalk.Analysis.Semantics;

public sealed class DestructorSymbol : IFunctionSymbol
{
	public bool IsStatic => false;
	public string SymbolTypeName => "a destructor";
	public Type? EvaluatedType => null;
	public bool IsConstant => false;
	public string Name { get; }
	public Scope Body { get; }

	public DestructorSymbol(Scope body)
	{
		Name = "$destructor";
		Body = body;
	}
}