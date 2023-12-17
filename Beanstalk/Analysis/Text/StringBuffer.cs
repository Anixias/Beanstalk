namespace Beanstalk.Analysis.Text;

public sealed class StringBuffer(string text) : IBuffer
{
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
}