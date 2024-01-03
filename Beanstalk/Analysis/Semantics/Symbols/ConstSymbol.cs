namespace Beanstalk.Analysis.Semantics;

public sealed class ConstSymbol : ISymbol
{
	public string SymbolTypeName => "a constant";
	public string Name { get; }
	public bool IsStatic { get; }
	public Type? EvaluatedType { get; set; }
	
	public ConstSymbol(string name, bool isStatic)
	{
		Name = name;
		IsStatic = isStatic;
	}
}