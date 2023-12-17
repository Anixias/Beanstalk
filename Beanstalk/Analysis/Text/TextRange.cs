namespace Beanstalk.Analysis.Text;

public readonly struct TextRange(int start, int end)
{
	public int Start { get; } = start;
	public int End { get; } = end;
	public int Length => End - Start;
}