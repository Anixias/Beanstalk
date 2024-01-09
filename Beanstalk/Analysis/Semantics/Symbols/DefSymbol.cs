namespace Beanstalk.Analysis.Semantics;

public sealed class DefSymbol : ISymbol
{
	public string Name { get; }
	public string SymbolTypeName => "a type alias";
	public bool IsConstant => true;
	public bool IsStatic => true;
	public Type? EvaluatedType { get; set; }
	
	public DefSymbol(string name)
	{
		Name = name;
	}
}