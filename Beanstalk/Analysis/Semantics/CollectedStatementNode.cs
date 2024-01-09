using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public abstract class CollectedStatementNode : ICollectedAstNode
{
	public interface IVisitor<out T>
	{
		T Visit(CollectedProgramStatement programStatement);
		T Visit(CollectedModuleStatement moduleStatement);
		T Visit(CollectedStructDeclarationStatement structDeclarationStatement);
		T Visit(CollectedFieldDeclarationStatement statement);
		T Visit(CollectedConstDeclarationStatement statement);
		T Visit(CollectedDefStatement statement);
		T Visit(CollectedEntryStatement entryStatement);
		T Visit(CollectedFunctionDeclarationStatement functionDeclarationStatement);
		T Visit(CollectedExternalFunctionStatement externalFunctionStatement);
		T Visit(CollectedConstructorDeclarationStatement statement);
		T Visit(CollectedDestructorDeclarationStatement statement);
		T Visit(CollectedStringDeclarationStatement statement);
		T Visit(CollectedCastDeclarationStatement statement);
		T Visit(CollectedOperatorDeclarationStatement statement);
		T Visit(CollectedExpressionStatement statement);
		T Visit(CollectedBlockStatement statement);
		T Visit(CollectedVarDeclarationStatement statement);
		T Visit(CollectedSimpleStatement statement);
	}
	
	public interface IVisitor
	{
		void Visit(CollectedProgramStatement statement);
		void Visit(CollectedModuleStatement statement);
		void Visit(CollectedStructDeclarationStatement structDeclarationStatement);
		void Visit(CollectedFieldDeclarationStatement statement);
		void Visit(CollectedConstDeclarationStatement statement);
		void Visit(CollectedDefStatement statement);
		void Visit(CollectedEntryStatement statement);
		void Visit(CollectedFunctionDeclarationStatement statement);
		void Visit(CollectedExternalFunctionStatement statement);
		void Visit(CollectedConstructorDeclarationStatement statement);
		void Visit(CollectedDestructorDeclarationStatement statement);
		void Visit(CollectedStringDeclarationStatement statement);
		void Visit(CollectedCastDeclarationStatement statement);
		void Visit(CollectedOperatorDeclarationStatement statement);
		void Visit(CollectedExpressionStatement statement);
		void Visit(CollectedBlockStatement statement);
		void Visit(CollectedVarDeclarationStatement statement);
		void Visit(CollectedSimpleStatement statement);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
}

public sealed class CollectedProgramStatement : CollectedStatementNode
{
	public SymbolTable? importedSymbols;
	public readonly ImmutableArray<StatementNode> importStatements;
	public readonly ModuleSymbol? moduleSymbol;
	public readonly ImmutableArray<CollectedStatementNode> topLevelStatements;

	public CollectedProgramStatement(IEnumerable<StatementNode> importStatements, ModuleSymbol? moduleSymbol,
		IEnumerable<CollectedStatementNode> topLevelStatements)
	{
		this.importStatements = importStatements.ToImmutableArray();
		this.moduleSymbol = moduleSymbol;
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

public sealed class CollectedModuleStatement : CollectedStatementNode
{
	public readonly ModuleSymbol moduleSymbol;
	public readonly ImmutableArray<CollectedStatementNode> topLevelStatements;
	
	public CollectedModuleStatement(ModuleSymbol moduleSymbol, IEnumerable<CollectedStatementNode> topLevelStatements)
	{
		this.moduleSymbol = moduleSymbol;
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

public sealed class CollectedStructDeclarationStatement : CollectedStatementNode
{
	public readonly StructSymbol structSymbol;
	public readonly ImmutableArray<CollectedStatementNode> statements;
	
	public CollectedStructDeclarationStatement(StructSymbol structSymbol, IEnumerable<CollectedStatementNode> statements)
	{
		this.structSymbol = structSymbol;
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

public sealed class CollectedFieldDeclarationStatement : CollectedStatementNode
{
	public readonly FieldSymbol fieldSymbol;
	public readonly SyntaxType? syntaxType;
	public readonly ExpressionNode? initializer;
	public readonly TextRange range;

	public CollectedFieldDeclarationStatement(FieldSymbol fieldSymbol, SyntaxType? syntaxType,
		ExpressionNode? initializer, TextRange range)
	{
		this.fieldSymbol = fieldSymbol;
		this.syntaxType = syntaxType;
		this.initializer = initializer;
		this.range = range;
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

public sealed class CollectedConstDeclarationStatement : CollectedStatementNode
{
	public readonly ConstSymbol constSymbol;
	public readonly SyntaxType? syntaxType;
	public readonly ExpressionNode initializer;
	public readonly TextRange range;

	public CollectedConstDeclarationStatement(ConstSymbol constSymbol, SyntaxType? syntaxType,
		ExpressionNode initializer, TextRange range)
	{
		this.constSymbol = constSymbol;
		this.syntaxType = syntaxType;
		this.initializer = initializer;
		this.range = range;
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

public sealed class CollectedDefStatement : CollectedStatementNode
{
	public readonly DefSymbol defSymbol;
	public readonly SyntaxType syntaxType;
	
	public CollectedDefStatement(DefSymbol defSymbol, SyntaxType syntaxType)
	{
		this.defSymbol = defSymbol;
		this.syntaxType = syntaxType;
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

public sealed class CollectedEntryStatement : CollectedStatementNode
{
	public EntrySymbol? entrySymbol;
	public readonly Scope scope;
	public readonly EntryStatement entryStatement;
	public readonly ImmutableArray<CollectedStatementNode> statements;

	public CollectedEntryStatement(EntryStatement entryStatement, Scope scope,
		IEnumerable<CollectedStatementNode> statements)
	{
		this.entryStatement = entryStatement;
		this.scope = scope;
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

public sealed class CollectedFunctionDeclarationStatement : CollectedStatementNode
{
	public FunctionSymbol? functionSymbol;
	public readonly FunctionDeclarationStatement functionDeclarationStatement;
	public readonly Scope scope;
	public readonly CollectedStatementNode body;

	public CollectedFunctionDeclarationStatement(FunctionDeclarationStatement functionDeclarationStatement, Scope scope,
		CollectedStatementNode body)
	{
		this.functionDeclarationStatement = functionDeclarationStatement;
		this.scope = scope;
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

public sealed class CollectedExternalFunctionStatement : CollectedStatementNode
{
	public ExternalFunctionSymbol? externalFunctionSymbol;
	public readonly ExternalFunctionStatement externalFunctionStatement;

	public CollectedExternalFunctionStatement(ExternalFunctionStatement externalFunctionStatement)
	{
		this.externalFunctionStatement = externalFunctionStatement;
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

public sealed class CollectedConstructorDeclarationStatement : CollectedStatementNode
{
	public ConstructorSymbol? constructorSymbol;
	public readonly ConstructorDeclarationStatement constructorDeclarationStatement;
	public readonly Scope scope;
	public readonly CollectedStatementNode body;

	public CollectedConstructorDeclarationStatement(ConstructorDeclarationStatement constructorDeclarationStatement,
		Scope scope, CollectedStatementNode body)
	{
		this.constructorDeclarationStatement = constructorDeclarationStatement;
		this.scope = scope;
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

public sealed class CollectedDestructorDeclarationStatement : CollectedStatementNode
{
	public DestructorSymbol? destructorSymbol;
	public readonly DestructorDeclarationStatement destructorDeclarationStatement;
	public readonly Scope scope;
	public readonly CollectedStatementNode body;

	public CollectedDestructorDeclarationStatement(DestructorDeclarationStatement destructorDeclarationStatement,
		Scope scope, CollectedStatementNode body)
	{
		this.destructorDeclarationStatement = destructorDeclarationStatement;
		this.scope = scope;
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

public sealed class CollectedCastDeclarationStatement : CollectedStatementNode
{
	public CastOverloadSymbol? castOverloadSymbol;
	public readonly CastDeclarationStatement castDeclarationStatement;
	public readonly Scope scope;
	public readonly CollectedStatementNode body;

	public CollectedCastDeclarationStatement(CastDeclarationStatement castDeclarationStatement, Scope scope,
		CollectedStatementNode body)
	{
		this.castDeclarationStatement = castDeclarationStatement;
		this.scope = scope;
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

public sealed class CollectedStringDeclarationStatement : CollectedStatementNode
{
	public StringFunctionSymbol? stringFunctionSymbol;
	public readonly StringDeclarationStatement stringDeclarationStatement;
	public readonly Scope scope;
	public readonly CollectedStatementNode body;

	public CollectedStringDeclarationStatement(StringDeclarationStatement stringDeclarationStatement, Scope scope,
		CollectedStatementNode body)
	{
		this.stringDeclarationStatement = stringDeclarationStatement;
		this.scope = scope;
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

public sealed class CollectedOperatorDeclarationStatement : CollectedStatementNode
{
	public OperatorOverloadSymbol? operatorOverloadSymbol;
	public readonly OperatorDeclarationStatement operatorDeclarationStatement;
	public readonly Scope scope;
	public readonly CollectedStatementNode body;

	public CollectedOperatorDeclarationStatement(OperatorDeclarationStatement operatorDeclarationStatement, Scope scope,
		CollectedStatementNode body)
	{
		this.operatorDeclarationStatement = operatorDeclarationStatement;
		this.scope = scope;
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

public sealed class CollectedExpressionStatement : CollectedStatementNode
{
	public readonly ExpressionStatement statement;

	public CollectedExpressionStatement(ExpressionStatement statement)
	{
		this.statement = statement;
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

public sealed class CollectedBlockStatement : CollectedStatementNode
{
	public readonly Scope scope;
	public readonly ImmutableArray<CollectedStatementNode> statements;

	public CollectedBlockStatement(Scope scope, IEnumerable<CollectedStatementNode> statements)
	{
		this.scope = scope;
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

public sealed class CollectedVarDeclarationStatement : CollectedStatementNode
{
	public readonly VarSymbol varSymbol;
	public readonly Token varToken;
	public readonly SyntaxType? syntaxType;
	public readonly ExpressionNode? initializer;

	public CollectedVarDeclarationStatement(VarSymbol varSymbol, Token varToken, SyntaxType? syntaxType,
		ExpressionNode? initializer)
	{
		this.varSymbol = varSymbol;
		this.varToken = varToken;
		this.initializer = initializer;
		this.syntaxType = syntaxType;
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

public sealed class CollectedSimpleStatement : CollectedStatementNode
{
	public readonly StatementNode statementNode;

	public CollectedSimpleStatement(StatementNode statementNode)
	{
		this.statementNode = statementNode;
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