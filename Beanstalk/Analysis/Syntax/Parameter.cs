using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public readonly struct Parameter
{
	public readonly Token identifier;
	public readonly Type type;
	public readonly ExpressionNode? defaultExpression;

	public Parameter(Token identifier, Type type, ExpressionNode? defaultExpression = null)
	{
		this.identifier = identifier;
		this.type = type;
		this.defaultExpression = defaultExpression;
	}
}