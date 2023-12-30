using System.Collections.Immutable;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Syntax;

public abstract class StatementNode : IAstNode
{
	public interface IVisitor<out T>
	{
		T Visit(ProgramStatement statement);
		T Visit(ImportStatement statement);
		T Visit(DllImportStatement statement);
		T Visit(ExternalFunctionStatement statement);
		T Visit(ModuleStatement statement);
		T Visit(EntryStatement statement);
		T Visit(FunctionDeclarationStatement statement);
		T Visit(ConstructorDeclarationStatement statement);
		T Visit(DestructorDeclarationStatement statement);
		T Visit(ExpressionStatement statement);
		T Visit(BlockStatement statement);
		T Visit(IfStatement statement);
		T Visit(MutableVarDeclarationStatement statement);
		T Visit(ImmutableVarDeclarationStatement statement);
		T Visit(ConstVarDeclarationStatement statement);
		T Visit(ReturnStatement statement);
		T Visit(StructDeclarationStatement statement);
		T Visit(InterfaceDeclarationStatement statement);
		T Visit(CastDeclarationStatement statement);
		T Visit(OperatorDeclarationStatement statement);
		T Visit(FieldDeclarationStatement statement);
		T Visit(DefineStatement statement);
	}
	
	public interface IVisitor
	{
		void Visit(ProgramStatement programStatement);
		void Visit(ImportStatement statement);
		void Visit(DllImportStatement statement);
		void Visit(ExternalFunctionStatement statement);
		void Visit(ModuleStatement statement);
		void Visit(EntryStatement statement);
		void Visit(FunctionDeclarationStatement statement);
		void Visit(ConstructorDeclarationStatement statement);
		void Visit(DestructorDeclarationStatement statement);
		void Visit(ExpressionStatement statement);
		void Visit(BlockStatement blockStatement);
		void Visit(IfStatement statement);
		void Visit(MutableVarDeclarationStatement statement);
		void Visit(ImmutableVarDeclarationStatement statement);
		void Visit(ConstVarDeclarationStatement statement);
		void Visit(ReturnStatement statement);
		void Visit(StructDeclarationStatement structDeclarationStatement);
		void Visit(InterfaceDeclarationStatement statement);
		void Visit(CastDeclarationStatement statement);
		void Visit(OperatorDeclarationStatement statement);
		void Visit(FieldDeclarationStatement statement);
		void Visit(DefineStatement statement);
	}
	
	public readonly TextRange range;

	protected StatementNode(TextRange range)
	{
		this.range = range;
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ImportStatement : StatementNode
{
	public readonly ModuleName scope;
	public readonly Token identifier;
	public readonly Token? alias;

	public ImportStatement(ModuleName scope, Token identifier, Token? alias, TextRange range) : base(range)
	{
		this.scope = scope;
		this.identifier = identifier;
		this.alias = alias;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class DllImportStatement : StatementNode
{
	public readonly string dllPath;
	public readonly ImmutableArray<StatementNode> statements;
	
	public DllImportStatement(string dllPath, IEnumerable<StatementNode> statements, TextRange range) : base(range)
	{
		this.dllPath = dllPath;
		this.statements = statements.ToImmutableArray();
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ModuleStatement : StatementNode
{
	// Todo: Track whether has a body or not -- alternatively, move all statements into this after parsing if file-scope
	public readonly ModuleName scope;
	public readonly ImmutableArray<StatementNode> topLevelStatements;

	public ModuleStatement(ModuleName scope, IEnumerable<StatementNode> topLevelStatements, TextRange range)
		: base(range)
	{
		this.scope = scope;
		this.topLevelStatements = topLevelStatements.ToImmutableArray();
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class FunctionDeclarationStatement : StatementNode
{
	public readonly Token identifier;
	public readonly ImmutableArray<Token> typeParameters;
	public readonly ImmutableArray<Parameter> parameters;
	public readonly SyntaxType? returnType;
	public readonly StatementNode body;

	public FunctionDeclarationStatement(Token identifier, IEnumerable<Token> typeParameters,
		IEnumerable<Parameter> parameters, SyntaxType? returnType, StatementNode body, TextRange range) : base(range)

	{
		this.identifier = identifier;
		this.typeParameters = typeParameters.ToImmutableArray();
		this.parameters = parameters.ToImmutableArray();
		this.returnType = returnType;
		this.body = body;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ExternalFunctionStatement : StatementNode
{
	public readonly Token identifier;
	public readonly IReadOnlyDictionary<string, string> attributes;
	public readonly ImmutableArray<Parameter> parameters;
	public readonly SyntaxType? returnType;

	public ExternalFunctionStatement(Token identifier, IEnumerable<Parameter> parameters, SyntaxType? returnType,
		IReadOnlyDictionary<string, string> attributes, TextRange range) : base(range)
	{
		this.identifier = identifier;
		this.parameters = parameters.ToImmutableArray();
		this.returnType = returnType;
		this.attributes = attributes;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class DestructorDeclarationStatement : StatementNode
{
	public readonly StatementNode body;

	public DestructorDeclarationStatement(StatementNode body, TextRange range) : base(range)
	{
		this.body = body;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ExpressionStatement : StatementNode
{
	public readonly ExpressionNode expression;

	public ExpressionStatement(ExpressionNode expression, TextRange range) : base(range)
	{
		this.expression = expression;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class BlockStatement : StatementNode
{
	public readonly ImmutableArray<StatementNode> statements;

	public BlockStatement(IEnumerable<StatementNode> statements, TextRange range) : base(range)
	{
		this.statements = statements.ToImmutableArray();
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public abstract class VarDeclarationStatement : StatementNode
{
	public virtual bool IsImmutable => true;
	public virtual bool IsConstant => false;
	public readonly Token identifier;
	public readonly SyntaxType? type;

	protected VarDeclarationStatement(Token identifier, SyntaxType? type, TextRange range) : base(range)
	{
		this.identifier = identifier;
		this.type = type;
	}
}

public sealed class MutableVarDeclarationStatement : VarDeclarationStatement
{
	public override bool IsImmutable => false;
	public readonly ExpressionNode? initializer;

	public MutableVarDeclarationStatement(Token identifier, SyntaxType? type, ExpressionNode? initializer, TextRange range)
		: base(identifier, type, range)
	{
		this.initializer = initializer;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ImmutableVarDeclarationStatement : VarDeclarationStatement
{
	public readonly ExpressionNode initializer;

	public ImmutableVarDeclarationStatement(Token identifier, SyntaxType? type, ExpressionNode initializer, TextRange range)
		: base(identifier, type, range)
	{
		this.initializer = initializer;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ConstVarDeclarationStatement : VarDeclarationStatement
{
	public override bool IsConstant => true;
	public readonly ExpressionNode initializer;

	public ConstVarDeclarationStatement(Token identifier, SyntaxType? type, ExpressionNode initializer, TextRange range)
		: base(identifier, type, range)
	{
		this.initializer = initializer;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class ReturnStatement : StatementNode
{
	public readonly ExpressionNode? expression;

	public ReturnStatement(ExpressionNode? expression, TextRange range) : base(range)
	{
		this.expression = expression;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class CastDeclarationStatement : StatementNode
{
	public readonly bool isImplicit;
	public readonly Parameter parameter;
	public readonly SyntaxType returnSyntaxType;
	public readonly StatementNode body;

	public CastDeclarationStatement(bool isImplicit, Parameter parameter, SyntaxType returnSyntaxType, StatementNode body, TextRange range) :
		base(range)
	{
		this.isImplicit = isImplicit;
		this.parameter = parameter;
		this.returnSyntaxType = returnSyntaxType;
		this.body = body;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class OperatorDeclarationStatement : StatementNode
{
	public readonly OperationExpression operation;
	public readonly SyntaxType returnSyntaxType;
	public readonly StatementNode body;

	public OperatorDeclarationStatement(OperationExpression operation, SyntaxType returnSyntaxType, StatementNode body,
		TextRange range) : base(range)
	{
		this.operation = operation;
		this.returnSyntaxType = returnSyntaxType;
		this.body = body;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
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
	public readonly SyntaxType? type;
	public readonly ExpressionNode? initializer;

	public FieldDeclarationStatement(Token identifier, Mutability mutability, bool isStatic, SyntaxType? type,
		ExpressionNode? initializer, TextRange range) : base(range)

	{
		this.identifier = identifier;
		this.mutability = mutability;
		this.isStatic = isStatic;
		this.type = type;
		this.initializer = initializer;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}

public sealed class DefineStatement : StatementNode
{
	public readonly Token identifier;
	public readonly SyntaxType type;
	
	public DefineStatement(Token identifier, SyntaxType type, TextRange range) : base(range)
	{
		this.identifier = identifier;
		this.type = type;
	}

	public override void Accept(IVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override T Accept<T>(IVisitor<T> visitor)
	{
		return visitor.Visit(this);
	}
}