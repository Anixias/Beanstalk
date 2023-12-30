using System.Collections.Immutable;
using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

public abstract class CollectedStatementNode : ICollectedAstNode
{
	public interface IVisitor<out T>
	{
		T Visit(CollectedProgramStatement statement);
		T Visit(CollectedModuleStatement statement);
		T Visit(CollectedStructStatement statement);
		T Visit(CollectedFieldDeclarationStatement statement);
		T Visit(CollectedConstDeclarationStatement statement);
		T Visit(CollectedDefStatement statement);
		T Visit(CollectedFunctionDeclarationStatement statement);
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
		void Visit(CollectedCastDeclarationStatement statement);
		void Visit(CollectedOperatorDeclarationStatement statement);
		void Visit(CollectedSimpleStatement statement);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
}

public sealed class CollectedProgramStatement : CollectedStatementNode
{
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
	
	public CollectedFieldDeclarationStatement(FieldSymbol fieldSymbol, SyntaxType? syntaxType)
	{
		this.fieldSymbol = fieldSymbol;
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

public sealed class CollectedConstDeclarationStatement : CollectedStatementNode
{
	public readonly ConstSymbol constSymbol;
	public readonly SyntaxType? syntaxType;
	
	public CollectedConstDeclarationStatement(ConstSymbol constSymbol, SyntaxType? syntaxType)
	{
		this.constSymbol = constSymbol;
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