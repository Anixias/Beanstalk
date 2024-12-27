using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public sealed class CollectedAst(ICollectedAstNode root, IBuffer source, string workingDirectory, string filePath)
{
	public ICollectedAstNode Root { get; } = root;
	public IBuffer Source { get; } = source;
	public string WorkingDirectory { get; } = workingDirectory;
	public string FilePath { get; } = filePath;
}