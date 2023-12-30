using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public readonly struct ModuleName
{
	public readonly ImmutableArray<Token> identifiers;
	public readonly string text;

	public ModuleName(IEnumerable<Token> identifiers)
	{
		this.identifiers = identifiers.ToImmutableArray();
		text = string.Join('.', this.identifiers.Select(i => i.Text));
	}

	public override string ToString()
	{
		return text;
	}
}