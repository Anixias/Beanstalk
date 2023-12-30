﻿using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public sealed class ParameterSymbol : ISymbol
{
	public string SymbolTypeName => "a parameter";
	public string Name => VarSymbol.Name;
	public VarSymbol VarSymbol { get; }
	public ExpressionNode? Expression { get; }
	
	public ParameterSymbol(VarSymbol varSymbol, ExpressionNode? expression)
	{
		VarSymbol = varSymbol;
		Expression = expression;
	}
}