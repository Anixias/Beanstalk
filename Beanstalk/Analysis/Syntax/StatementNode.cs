using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class StatementNode : IAstNode
{
	
}

public sealed class ProgramStatement : StatementNode
{
	public readonly ImmutableArray<ImportStatement> importStatements;
	public readonly ModuleStatement? moduleStatement;
	public readonly ImmutableArray<StatementNode> topLevelStatements;

	public ProgramStatement(IEnumerable<ImportStatement> importStatements, ModuleStatement? moduleStatement,
		IEnumerable<StatementNode> topLevelStatements)
	{
		this.importStatements = importStatements.ToImmutableArray();
		this.moduleStatement = moduleStatement;
		this.topLevelStatements = topLevelStatements.ToImmutableArray();
	}
}

public sealed class ImportStatement : StatementNode
{
	public readonly ImmutableArray<Token> scope;
	public readonly Token identifier;

	public ImportStatement(IEnumerable<Token> scope, Token identifier)
	{
		this.scope = scope.ToImmutableArray();
		this.identifier = identifier;
	}
}

public sealed class ModuleStatement : StatementNode
{
	public readonly ImmutableArray<Token> scope;
	public readonly ImmutableArray<StatementNode> topLevelStatements;

	public ModuleStatement(IEnumerable<Token> scope, IEnumerable<StatementNode> topLevelStatements)
	{
		this.scope = scope.ToImmutableArray();
		this.topLevelStatements = topLevelStatements.ToImmutableArray();
	}
}

public sealed class EntryStatement : StatementNode
{
	// Todo: Arguments
	public readonly BlockStatement body;

	public EntryStatement(BlockStatement body)
	{
		this.body = body;
	}
}

public sealed class BlockStatement : StatementNode
{
	public readonly ImmutableArray<StatementNode> statements;

	public BlockStatement(IEnumerable<StatementNode> statements)
	{
		this.statements = statements.ToImmutableArray();
	}
}