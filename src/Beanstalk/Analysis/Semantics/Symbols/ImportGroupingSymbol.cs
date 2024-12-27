namespace Beanstalk.Analysis.Semantics;

public sealed class ImportGroupingSymbol : ISymbol
{
	public string SymbolTypeName => "an import grouping";
	public Type? EvaluatedType => null;
	public bool IsConstant => false;
	public bool IsStatic => false;
	public string Name { get; }
	public SymbolTable Symbols { get; } = new();
	
	public ImportGroupingSymbol(string name)
	{
		Name = name;
	}
}