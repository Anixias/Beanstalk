namespace Beanstalk.Analysis.Semantics;

public sealed class ModuleSymbol : ISymbol
{
	public string SymbolTypeName => "a module";
	public Type? EvaluatedType => null;
	public string Name { get; }
	public Scope Scope { get; }
	
	public ModuleSymbol(string name, Scope scope)
	{
		Name = name;
		Scope = scope;
	}
}