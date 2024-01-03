namespace Beanstalk.Analysis.Text;

public sealed class StringBuffer(string text) : IBuffer
{
	public static readonly IBuffer Empty = new StringBuffer("");
	public char this[int position] => text[position];
	public int Length => text.Length;

	public string GetText()
	{
		return text;
	}

	public string GetText(TextRange range)
	{
		return text.Substring(range.Start, range.Length);
	}

	public (int, int) GetLineColumn(int position)
	{
		if (position < 0 || position > text.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(position));
		}

		var line = 1;
		var column = 1;

		for (var i = 0; i < position; i++)
		{
			switch (text[i])
			{
				case '\n':
					line++;
					column = 1;
					break;
				case '\r':
				{
					if (i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}

					line++;
					column = 1;
					break;
				}
				default:
					column++;
					break;
			}
		}

		return (line, column);
	}
}