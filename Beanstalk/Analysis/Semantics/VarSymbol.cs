namespace Beanstalk.Analysis.Semantics;

public sealed class VarSymbol : ISymbol
{
	public string SymbolTypeName => "a local variable";
	public string Name { get; }
	public bool IsMutable { get; }
	public Type? Type { get; set; }
	
	public VarSymbol(string name, bool isMutable)
	{
		Name = name;
		IsMutable = isMutable;
	}
}