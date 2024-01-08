namespace Beanstalk.Analysis.Semantics;

public sealed class FieldSymbol : ISymbol
{
	public string SymbolTypeName => "a field";
	public string Name { get; }
	public bool IsMutable { get; }
	public bool IsStatic { get; }
	public TypeSymbol Owner { get; }
	public uint Index { get; }
	public ResolvedExpressionNode? Initializer { get; set; } = null;
	public Type? EvaluatedType { get; set; }
	
	public FieldSymbol(string name, bool isMutable, bool isStatic, TypeSymbol owner, uint index)
	{
		Name = name;
		IsMutable = isMutable;
		IsStatic = isStatic;
		Owner = owner;
		Index = index;
	}
}