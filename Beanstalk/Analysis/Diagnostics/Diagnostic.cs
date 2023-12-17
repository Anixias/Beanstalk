using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Diagnostics;

public readonly struct Diagnostic(TextRange? range, string message)
{
	public TextRange? Range { get; } = range;
	public string Message { get; } = message;

	public override string ToString()
	{
		return Range is not { } textRange ? Message : $"[{textRange.Start}:{textRange.End}] {Message}";
	}
}