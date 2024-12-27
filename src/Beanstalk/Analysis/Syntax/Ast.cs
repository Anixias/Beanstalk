using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public class Ast(IAstNode root, IBuffer source)
{
	public IAstNode Root { get; } = root;
	public IBuffer Source { get; } = source;
}