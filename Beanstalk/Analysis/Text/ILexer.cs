﻿namespace Beanstalk.Analysis.Text;

public readonly struct ScanResult(Token token, int nextPosition)
{
	public Token Token { get; } = token;
	public int NextPosition { get; } = nextPosition;
}

public interface ILexer : IEnumerable<Token>
{
	ScanResult? ScanToken(int position);
}