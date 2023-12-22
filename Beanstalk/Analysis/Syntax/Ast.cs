namespace Beanstalk.Analysis.Syntax;

public class Ast(IAstNode root)
{
	public IAstNode Root { get; } = root;
}