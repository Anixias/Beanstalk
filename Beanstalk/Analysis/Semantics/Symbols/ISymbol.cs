namespace Beanstalk.Analysis.Semantics;

public interface ISymbol
{
	string Name { get; }
	string SymbolTypeName { get; }
	Type? EvaluatedType { get; }
}

public interface IFunctionSymbol : ISymbol
{
	bool IsStatic { get; }
}