namespace Beanstalk.Analysis.Semantics;

public sealed class ConstSymbol : ISymbol
{
	public string SymbolTypeName => "a constant";
	public bool IsConstant => true;
	public bool IsStatic => true;
	public string Name { get; }
	public Type? EvaluatedType { get; set; }
	
	public ConstSymbol(string name)
	{
		Name = name;
	}
}