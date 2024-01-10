using System.Collections;
// ReSharper disable UseCollectionExpression

namespace Beanstalk.Analysis.Diagnostics;

public sealed class DiagnosticList : IEnumerable<Diagnostic>
{
	public int Count => diagnostics.Count;

	public DiagnosticList Errors => new()
	{
		diagnostics.Where(d => d.severity == DiagnosticSeverity.Error).ToList()
	};

	public DiagnosticList Warnings => new()
	{
		diagnostics.Where(d => d.severity == DiagnosticSeverity.Warning).ToList()
	};
	
	public int ErrorCount => diagnostics.Count(d => d.severity == DiagnosticSeverity.Error);
	public int WarningCount => diagnostics.Count(d => d.severity == DiagnosticSeverity.Warning);
	
	private readonly List<Diagnostic> diagnostics = [];
	
	public void Add(Diagnostic diagnostic)
	{
		diagnostics.Add(diagnostic);
	}
	
	public void Add(IEnumerable<Diagnostic> diagnostic)
	{
		diagnostics.AddRange(diagnostic);
	}
	
	public IEnumerator<Diagnostic> GetEnumerator()
	{
		return diagnostics.OrderBy(d => d.line).ThenBy(d => d.column).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}