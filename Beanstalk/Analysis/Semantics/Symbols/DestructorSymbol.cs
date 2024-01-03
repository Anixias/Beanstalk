namespace Beanstalk.Analysis.Semantics;

public sealed class DestructorSymbol : ISymbol
{
	public string SymbolTypeName => "a destructor";
	public Type? EvaluatedType => null;
	public string Name { get; }
	public Scope Body { get; }

	public DestructorSymbol(Scope body)
	{
		Name = "$destructor";
		Body = body;
	}
}