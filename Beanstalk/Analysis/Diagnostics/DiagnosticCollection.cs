using System.Collections;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Diagnostics;

public sealed class DiagnosticCollection : IEnumerable<Diagnostic>
{
	private readonly List<Diagnostic> diagnostics = [];

	public void Report(TextRange range, string message)
	{
		diagnostics.Add(new Diagnostic(range, message));
	}
	
	public void Report(string message)
	{
		diagnostics.Add(new Diagnostic(null, message));
	}
	
	public IEnumerator<Diagnostic> GetEnumerator()
	{
		return diagnostics.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void AppendDiagnostics(DiagnosticCollection diagnosticCollection)
	{
		diagnostics.AddRange(diagnosticCollection.diagnostics);
	}
}