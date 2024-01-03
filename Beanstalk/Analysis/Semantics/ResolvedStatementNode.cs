using System.Collections.Immutable;

namespace Beanstalk.Analysis.Semantics;

public abstract class ResolvedStatementNode : IResolvedAstNode
{
	public interface IVisitor<out T>
	{
		T Visit(ResolvedProgramStatement statement);
		T Visit(ResolvedModuleStatement statement);
		T Visit(ResolvedStructDeclarationStatement structDeclarationStatement);
		T Visit(ResolvedFieldDeclarationStatement statement);
		T Visit(ResolvedConstDeclarationStatement statement);
		T Visit(ResolvedFunctionDeclarationStatement statement);
		T Visit(ResolvedConstructorDeclarationStatement statement);
		T Visit(ResolvedDestructorDeclarationStatement statement);
		T Visit(ResolvedSimpleStatement statement);
	}
	
	public interface IVisitor
	{
		void Visit(ResolvedProgramStatement statement);
		void Visit(ResolvedModuleStatement statement);
		void Visit(ResolvedStructDeclarationStatement structDeclarationStatement);
		void Visit(ResolvedFieldDeclarationStatement statement);
		void Visit(ResolvedConstDeclarationStatement statement);
		void Visit(ResolvedFunctionDeclarationStatement statement);
		void Visit(ResolvedConstructorDeclarationStatement statement);
		void Visit(ResolvedDestructorDeclarationStatement statement);
		void Visit(ResolvedSimpleStatement statement);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);
}

public sealed class ResolvedProgramStatement : ResolvedStatementNode
{
	public readonly ModuleSymbol? moduleSymbol;
	public readonly ImmutableArray<ResolvedStatementNode> topLevelStatements;

	public ResolvedProgramStatement(ModuleSymbol? moduleSymbol, IEnumerable<ResolvedStatementNode> topLevelStatements)
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

public sealed class ResolvedModuleStatement : ResolvedStatementNode
{
	public readonly ModuleSymbol moduleSymbol;
	public readonly ImmutableArray<ResolvedStatementNode> topLevelStatements;
	
	public ResolvedModuleStatement(ModuleSymbol moduleSymbol, IEnumerable<ResolvedStatementNode> topLevelStatements)
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

public sealed class ResolvedStructDeclarationStatement : ResolvedStatementNode
{
	public readonly StructSymbol structSymbol;
	public readonly ImmutableArray<ResolvedStatementNode> statements;
	
	public ResolvedStructDeclarationStatement(StructSymbol structSymbol, IEnumerable<ResolvedStatementNode> statements)
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

public sealed class ResolvedFieldDeclarationStatement : ResolvedStatementNode
{
	public readonly FieldSymbol fieldSymbol;
	public readonly ResolvedExpressionNode? initializer;
	
	public ResolvedFieldDeclarationStatement(FieldSymbol fieldSymbol, ResolvedExpressionNode? initializer)
	{
		this.fieldSymbol = fieldSymbol;
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

public sealed class ResolvedConstDeclarationStatement : ResolvedStatementNode
{
	public readonly ConstSymbol constSymbol;
	public readonly ResolvedExpressionNode initializer;
	
	public ResolvedConstDeclarationStatement(ConstSymbol constSymbol, ResolvedExpressionNode initializer)
	{
		this.constSymbol = constSymbol;
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

public sealed class ResolvedFunctionDeclarationStatement : ResolvedStatementNode
{
	public readonly FunctionSymbol functionSymbol;
	
	public ResolvedFunctionDeclarationStatement(FunctionSymbol functionSymbol)
	{
		this.functionSymbol = functionSymbol;
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

public sealed class ResolvedConstructorDeclarationStatement : ResolvedStatementNode
{
	public readonly ConstructorSymbol constructorSymbol;
	
	public ResolvedConstructorDeclarationStatement(ConstructorSymbol constructorSymbol)
	{
		this.constructorSymbol = constructorSymbol;
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

public sealed class ResolvedDestructorDeclarationStatement : ResolvedStatementNode
{
	public readonly DestructorSymbol destructorSymbol;
	
	public ResolvedDestructorDeclarationStatement(DestructorSymbol destructorSymbol)
	{
		this.destructorSymbol = destructorSymbol;
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

public sealed class ResolvedSimpleStatement : ResolvedStatementNode
{
	public readonly CollectedStatementNode statement;
	
	public ResolvedSimpleStatement(CollectedStatementNode statement)
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