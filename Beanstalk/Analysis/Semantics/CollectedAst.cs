namespace Beanstalk.Analysis.Semantics;

public sealed class CollectedAst(ICollectedAstNode root, string workingDirectory, string filePath)
{
	public ICollectedAstNode Root { get; } = root;
	public string WorkingDirectory { get; } = workingDirectory;
	public string FilePath { get; } = filePath;
}