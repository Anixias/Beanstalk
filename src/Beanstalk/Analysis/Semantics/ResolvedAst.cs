namespace Beanstalk.Analysis.Semantics;

public sealed class ResolvedAst(IResolvedAstNode root, string workingDirectory, string filePath)
{
	public IResolvedAstNode Root { get; } = root;
	public string WorkingDirectory { get; } = workingDirectory;
	public string FilePath { get; } = filePath;
}