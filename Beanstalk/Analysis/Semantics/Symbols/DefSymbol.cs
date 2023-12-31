using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public sealed class DefSymbol : ISymbol
{
	public string Name { get; }
	public string SymbolTypeName => "a type alias";
	public Type? Type { get; set; }
	
	public DefSymbol(string name)
	{
		Name = name;
	}
}