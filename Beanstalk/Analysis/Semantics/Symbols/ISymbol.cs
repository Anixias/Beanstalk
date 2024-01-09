namespace Beanstalk.Analysis.Semantics;

public interface ISymbol
{
	string Name { get; }
	string SymbolTypeName { get; }
	Type? EvaluatedType { get; }
	bool IsConstant { get; }
	bool IsStatic { get; }
}

public interface IFunctionSymbol : ISymbol
{
}