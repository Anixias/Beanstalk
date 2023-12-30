namespace Beanstalk.Analysis.Semantics;

public sealed class ResolvedAst(IResolvedAstNode root)
{
	public IResolvedAstNode Root { get; } = root;
}