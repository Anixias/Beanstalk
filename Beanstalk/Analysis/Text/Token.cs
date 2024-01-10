using Beanstalk.Analysis.Semantics;

namespace Beanstalk.Analysis.Text;

public sealed class Token(TokenType type, TextRange range, IBuffer source, object? value = null)
{
	public IBuffer Source { get; } = source;
	public TokenType Type { get; } = type;
	public TextRange Range { get; } = range;
	public object? Value { get; } = value;
	public ISymbol? Symbol { get; set; } = null;
	public string Text => Source.GetText(Range);
	private (int, int) LineColumn { get; } = source.GetLineColumn(range.Start);
	public int Line => LineColumn.Item1;
	public int Column => LineColumn.Item2;

	public override string ToString()
	{
		if (Value is null)
			return $"{Type}: {Text}";
		
		return $"{Type}: {Text} ({Value})";
	}
}