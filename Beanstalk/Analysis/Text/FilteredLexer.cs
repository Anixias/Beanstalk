﻿using System.Collections;

namespace Beanstalk.Analysis.Text;

public sealed class FilteredLexer(IBuffer source) : ILexer
{
	private readonly Lexer lexer = new(source);

	public ScanResult? ScanToken(int position)
	{
		var result = lexer.ScanToken(position);
		while (result is { Token.Type.IsFiltered: true } scanResult)
		{
			result = lexer.ScanToken(scanResult.NextPosition);
		}

		return result;
	}

	private List<Token> ScanAllTokens()
	{
		var tokens = new List<Token>();
		var position = 0;
		
		while (true)
		{
			if (ScanToken(position) is not { } lexerResult)
			{
				return tokens;
			}

			tokens.Add(lexerResult.Token);
			position = lexerResult.NextPosition;
		}
	}

	public IEnumerator<Token> GetEnumerator()
	{
		return ScanAllTokens().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}