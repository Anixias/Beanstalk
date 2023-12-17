namespace Beanstalk.Analysis.Text;

public sealed class Token(TokenType type, TextRange range, IBuffer source, object? value = null)
{
	private IBuffer Source { get; } = source;
	public TokenType Type { get; } = type;
	public TextRange Range { get; } = range;
	public object? Value { get; } = value;
	public string Text => Source.GetText(Range);

	public override string ToString()
	{
		return Text;
	}
}