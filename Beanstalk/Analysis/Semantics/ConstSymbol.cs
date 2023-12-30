namespace Beanstalk.Analysis.Semantics;

public sealed class ConstSymbol : ISymbol
{
	public string SymbolTypeName => "a constant";
	public string Name { get; }
	public bool IsStatic { get; }
	public Type? Type { get; set; }
	
	public ConstSymbol(string name, bool isStatic)
	{
		Name = name;
		IsStatic = isStatic;
	}
}