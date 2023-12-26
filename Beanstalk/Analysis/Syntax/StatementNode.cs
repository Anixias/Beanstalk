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

public sealed class FunctionDeclarationStatement : StatementNode
{
	public readonly Token identifier;
	public readonly ImmutableArray<Type> typeParameters;
	public readonly ImmutableArray<Parameter> parameters;
	public readonly Type? returnType;
	public readonly StatementNode body;

	public FunctionDeclarationStatement(Token identifier, IEnumerable<Type> typeParameters,
		IEnumerable<Parameter> parameters, Type? returnType, StatementNode body, TextRange range) : base(range)

	{
		this.identifier = identifier;
		this.typeParameters = typeParameters.ToImmutableArray();
		this.parameters = parameters.ToImmutableArray();
		this.returnType = returnType;
		this.body = body;
	}
}

public sealed class ConstructorDeclarationStatement : StatementNode
{
	public readonly ImmutableArray<Parameter> parameters;
	public readonly StatementNode body;

	public ConstructorDeclarationStatement(IEnumerable<Parameter> parameters, StatementNode body, TextRange range) :
		base(range)
	{
		this.parameters = parameters.ToImmutableArray();
		this.body = body;
	}
}

public sealed class DestructorDeclarationStatement : StatementNode
{
	public readonly StatementNode body;

	public DestructorDeclarationStatement(StatementNode body, TextRange range) : base(range)
	{
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

public sealed class IfStatement : StatementNode
{
	public readonly ExpressionNode condition;
	public readonly StatementNode thenBranch;
	public readonly StatementNode? elseBranch;

	public IfStatement(ExpressionNode condition, StatementNode thenBranch, StatementNode? elseBranch,
		TextRange range) : base(range)

	{
		this.condition = condition;
		this.thenBranch = thenBranch;
		this.elseBranch = elseBranch;
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

public sealed class StructDeclarationStatement : StatementNode
{
	public readonly Token identifier;
	public readonly bool isMutable;
	public readonly ImmutableArray<StatementNode> statements;

	public StructDeclarationStatement(Token identifier, bool isMutable, IEnumerable<StatementNode> statements,
		TextRange range) : base(range)

	{
		this.identifier = identifier;
		this.isMutable = isMutable;
		this.statements = statements.ToImmutableArray();
	}
}

public sealed class InterfaceDeclarationStatement : StatementNode
{
	public readonly Token identifier;
	public readonly ImmutableArray<StatementNode> statements;

	public InterfaceDeclarationStatement(Token identifier, IEnumerable<StatementNode> statements, TextRange range) :
		base(range)

	{
		this.identifier = identifier;
		this.statements = statements.ToImmutableArray();
	}
}

public sealed class CastDeclarationStatement : StatementNode
{
	public readonly bool isImplicit;
	public readonly Parameter parameter;
	public readonly Type returnType;
	public readonly StatementNode body;

	public CastDeclarationStatement(bool isImplicit, Parameter parameter, Type returnType, StatementNode body, TextRange range) :
		base(range)
	{
		this.isImplicit = isImplicit;
		this.parameter = parameter;
		this.returnType = returnType;
		this.body = body;
	}
}

public sealed class OperatorDeclarationStatement : StatementNode
{
	public readonly OperationExpression operation;
	public readonly Type returnType;
	public readonly StatementNode body;

	public OperatorDeclarationStatement(OperationExpression operation, Type returnType, StatementNode body, TextRange range) :
		base(range)
	{
		this.operation = operation;
		this.returnType = returnType;
		this.body = body;
	}
}

public sealed class FieldDeclarationStatement : StatementNode
{
	public enum Mutability
	{
		Mutable,
		Immutable,
		Constant
	}
	
	public readonly Token identifier;
	public readonly Mutability mutability;
	public readonly bool isStatic;
	public readonly Type? type;
	public readonly ExpressionNode? initializer;

	public FieldDeclarationStatement(Token identifier, Mutability mutability, bool isStatic, Type? type,
		ExpressionNode? initializer, TextRange range) : base(range)

	{
		this.identifier = identifier;
		this.mutability = mutability;
		this.isStatic = isStatic;
		this.type = type;
		this.initializer = initializer;
	}
}