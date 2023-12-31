namespace Beanstalk.Analysis.Semantics;

public sealed class FieldSymbol : ISymbol
{
	public string SymbolTypeName => "a field";
	public string Name { get; }
	public bool IsMutable { get; }
	public bool IsStatic { get; }
	public Type? Type { get; set; }
	
	public FieldSymbol(string name, bool isMutable, bool isStatic)
	{
		Name = name;
		IsMutable = isMutable;
		IsStatic = isStatic;
	}
}