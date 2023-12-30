using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public readonly struct Parameter
{
	public readonly Token identifier;
	public readonly SyntaxType? type;
	public readonly ExpressionNode? defaultExpression;
	public readonly bool isVariadic;
	public readonly bool isMutable;
	public readonly TextRange range;

	public Parameter(Token identifier, SyntaxType? type, ExpressionNode? defaultExpression, bool isVariadic,
		bool isMutable, TextRange range)
	{
		this.identifier = identifier;
		this.type = type;
		this.defaultExpression = defaultExpression;
		this.isVariadic = isVariadic;
		this.isMutable = isMutable;
		this.range = range;
	}

	public override string ToString()
	{
		if (type is not null)
			return $"{identifier.Text}:{type}";

		return identifier.Text;
	}
}