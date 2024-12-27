namespace Beanstalk.Analysis.Semantics;

public sealed class VarSymbol : ISymbol
{
	public bool IsConstant => false;
	public string SymbolTypeName => "a local variable";
	public string Name { get; }
	public bool IsMutable { get; }
	public bool IsStatic { get; set; } = false;
	public Type? EvaluatedType { get; set; }
	
	public VarSymbol(string name, bool isMutable)
	{
		Name = name;
		IsMutable = isMutable;
	}
}