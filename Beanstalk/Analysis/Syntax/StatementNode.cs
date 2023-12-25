using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class StatementNode : IAstNode
{
	public readonly TextRange range;

	protected StatementNode(TextRange range)
	{
		this.range = range;
	}
}

public sealed class ProgramStatement : StatementNode
{
	public readonly ImmutableArray<ImportStatement> importStatements;
	public readonly ModuleStatement? moduleStatement;
	public readonly ImmutableArray<StatementNode> topLevelStatements;

	public ProgramStatement(IEnumerable<ImportStatement> importStatements, ModuleStatement? moduleStatement,
		IEnumerable<StatementNode> topLevelStatements, TextRange range) : base(range)
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
	public readonly Token? alias;

	public ImportStatement(IEnumerable<Token> scope, Token identifier, Token? alias, TextRange range) : base(range)
	{
		this.scope = scope.ToImmutableArray();
		this.identifier = identifier;
		this.alias = alias;
	}
}

public sealed class ModuleStatement : StatementNode
{
	public readonly ImmutableArray<Token> scope;
	public readonly ImmutableArray<StatementNode> topLevelStatements;

	public ModuleStatement(IEnumerable<Token> scope, IEnumerable<StatementNode> topLevelStatements, TextRange range)
		: base(range)
	{
		this.scope = scope.ToImmutableArray();
		this.topLevelStatements = topLevelStatements.ToImmutableArray();
	}
}

public sealed class EntryStatement : StatementNode
{
	public readonly ImmutableArray<Parameter> parameters;
	public readonly BlockStatement body;

	public EntryStatement(IEnumerable<Parameter> parameters, BlockStatement body, TextRange range) : base(range)
	{
		this.parameters = parameters.ToImmutableArray();
		this.body = body;
	}
}

public sealed class ExpressionStatement : StatementNode
{
	public readonly ExpressionNode expression;

	public ExpressionStatement(ExpressionNode expression, TextRange range) : base(range)
	{
		this.expression = expression;
	}
}

public sealed class BlockStatement : StatementNode
{
	public readonly ImmutableArray<StatementNode> statements;

	public BlockStatement(IEnumerable<StatementNode> statements, TextRange range) : base(range)
	{
		this.statements = statements.ToImmutableArray();
	}
}

public abstract class VarDeclarationStatement : StatementNode
{
	public virtual bool IsImmutable => true;
	public virtual bool IsConstant => false;
	public readonly Token identifier;
	public readonly Type? type;

	protected VarDeclarationStatement(Token identifier, Type? type, TextRange range) : base(range)
	{
		this.identifier = identifier;
		this.type = type;
	}
}

public sealed class MutableVarDeclarationStatement : VarDeclarationStatement
{
	public override bool IsImmutable => false;
	public readonly ExpressionNode? initializer;

	public MutableVarDeclarationStatement(Token identifier, Type? type, ExpressionNode? initializer, TextRange range)
		: base(identifier, type, range)
	{
		this.initializer = initializer;
	}
}

public sealed class ImmutableVarDeclarationStatement : VarDeclarationStatement
{
	public readonly ExpressionNode initializer;

	public ImmutableVarDeclarationStatement(Token identifier, Type? type, ExpressionNode initializer, TextRange range)
		: base(identifier, type, range)
	{
		this.initializer = initializer;
	}
}

public sealed class ConstVarDeclarationStatement : VarDeclarationStatement
{
	public override bool IsConstant => true;
	public readonly ExpressionNode initializer;

	public ConstVarDeclarationStatement(Token identifier, Type? type, ExpressionNode initializer, TextRange range)
		: base(identifier, type, range)
	{
		this.initializer = initializer;
	}
}

public sealed class ReturnStatement : StatementNode
{
	public readonly ExpressionNode expression;

	public ReturnStatement(ExpressionNode expression, TextRange range) : base(range)
	{
		this.expression = expression;
	}
}