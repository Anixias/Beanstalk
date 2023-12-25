﻿namespace Beanstalk.Analysis.Text;

public readonly struct TextRange(int start, int end)
{
	public int Start { get; } = start;
	public int End { get; } = end;
	public int Length => End - Start;

	public TextRange Join(TextRange range)
	{
		return new TextRange(Math.Min(Start, range.Start), Math.Max(End, range.End));
	}
}