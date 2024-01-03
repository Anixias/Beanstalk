using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public abstract class CollectedStatementNode : ICollectedAstNode
{
	public interface IVisitor<out T>
	{
		T Visit(CollectedProgramStatement programStatement);
		T Visit(CollectedModuleStatement moduleStatement);
		T Visit(CollectedStructStatement structStatement);
		T Visit(CollectedFieldDeclarationStatement statement);
		T Visit(CollectedConstDeclarationStatement statement);
		T Visit(CollectedDefStatement statement);
		T Visit(CollectedFunctionDeclarationStatement statement);
		T Visit(CollectedConstructorDeclarationStatement statement);
		T Visit(CollectedDestructorDeclarationStatement statement);
		T Visit(CollectedCastDeclarationStatement statement);
		T Visit(CollectedOperatorDeclarationStatement statement);
		T Visit(CollectedSimpleStatement statement);
	}
	
	public interface IVisitor
	{
		void Visit(CollectedProgramStatement statement);
		void Visit(CollectedModuleStatement statement);
		void Visit(CollectedStructStatement structStatement);
		void Visit(CollectedFieldDeclarationStatement statement);
		void Visit(CollectedConstDeclarationStatement statement);
		void Visit(CollectedDefStatement statement);
		void Visit(CollectedFunctionDeclarationStatement statement);
		void Visit(CollectedConstructorDeclarationStatement statement);
		void Visit(CollectedDestructorDeclarationStatement statement);
		void Visit(CollectedCastDeclarationStatement statement);
		void Visit(CollectedOperatorDeclarationStatement statement);
		void Visit(CollectedSimpleStatement statement);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
}

public sealed class CollectedProgramStatement : CollectedStatementNode
{
	public SymbolTable? importedSymbols;
	public readonly ImmutableArray<ImportStatement> importStatements;
	public readonly ModuleSymbol? moduleSymbol;
	public readonly ImmutableArray<CollectedStatementNode> topLevelStatements;

	public CollectedProgramStatement(IEnumerable<ImportStatement> importStatements, ModuleSymbol? moduleSymbol,
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

public sealed class CollectedStructStatement : CollectedStatementNode
{
	public readonly StructSymbol structSymbol;
	public readonly ImmutableArray<CollectedStatementNode> statements;
	
	public CollectedStructStatement(StructSymbol structSymbol, IEnumerable<CollectedStatementNode> statements)
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

	public CollectedFieldDeclarationStatement(FieldSymbol fieldSymbol, SyntaxType? syntaxType,
		ExpressionNode? initializer)
	{
		this.fieldSymbol = fieldSymbol;
		this.syntaxType = syntaxType;
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

public sealed class CollectedConstDeclarationStatement : CollectedStatementNode
{
	public readonly ConstSymbol constSymbol;
	public readonly SyntaxType? syntaxType;
	public readonly ExpressionNode initializer;

	public CollectedConstDeclarationStatement(ConstSymbol constSymbol, SyntaxType? syntaxType,
		ExpressionNode initializer)
	{
		this.constSymbol = constSymbol;
		this.syntaxType = syntaxType;
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

public sealed class CollectedFunctionDeclarationStatement : CollectedStatementNode
{
	public FunctionSymbol? functionSymbol;
	public readonly FunctionDeclarationStatement functionDeclarationStatement;
	
	public CollectedFunctionDeclarationStatement(FunctionDeclarationStatement functionDeclarationStatement)
	{
		this.functionDeclarationStatement = functionDeclarationStatement;
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
	
	public CollectedConstructorDeclarationStatement(ConstructorDeclarationStatement constructorDeclarationStatement)
	{
		this.constructorDeclarationStatement = constructorDeclarationStatement;
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
	
	public CollectedDestructorDeclarationStatement(DestructorDeclarationStatement destructorDeclarationStatement)
	{
		this.destructorDeclarationStatement = destructorDeclarationStatement;
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
	
	public CollectedCastDeclarationStatement(CastDeclarationStatement castDeclarationStatement)
	{
		this.castDeclarationStatement = castDeclarationStatement;
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
	
	public CollectedOperatorDeclarationStatement(OperatorDeclarationStatement operatorDeclarationStatement)
	{
		this.operatorDeclarationStatement = operatorDeclarationStatement;
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