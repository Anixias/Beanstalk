using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public readonly struct Parameter
{
	public readonly Token identifier;
	public readonly Type? type;
	public readonly ExpressionNode? defaultExpression;
	public readonly TextRange range;

	public Parameter(Token identifier, Type? type, ExpressionNode? defaultExpression, TextRange range)
	{
		this.identifier = identifier;
		this.type = type;
		this.range = range;
		this.defaultExpression = defaultExpression;
	}

	public override string ToString()
	{
		if (type is not null)
			return $"{identifier.Text}:{type}";

		return identifier.Text;
	}
}