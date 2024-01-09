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
		T Visit(ResolvedEntryStatement statement);
		T Visit(ResolvedExternalFunctionStatement statement);
		T Visit(ResolvedFunctionDeclarationStatement statement);
		T Visit(ResolvedConstructorDeclarationStatement statement);
		T Visit(ResolvedDestructorDeclarationStatement statement);
		T Visit(ResolvedStringDeclarationStatement statement);
		T Visit(ResolvedOperatorDeclarationStatement statement);
		T Visit(ResolvedExpressionStatement statement);
		T Visit(ResolvedReturnStatement statement);
		T Visit(ResolvedBlockStatement statement);
		T Visit(ResolvedVarDeclarationStatement statement);
		T Visit(ResolvedSimpleStatement statement);
	}
	
	public interface IVisitor
	{
		void Visit(ResolvedProgramStatement statement);
		void Visit(ResolvedModuleStatement statement);
		void Visit(ResolvedStructDeclarationStatement structDeclarationStatement);
		void Visit(ResolvedFieldDeclarationStatement statement);
		void Visit(ResolvedConstDeclarationStatement statement);
		void Visit(ResolvedEntryStatement entryStatement);
		void Visit(ResolvedExternalFunctionStatement statement);
		void Visit(ResolvedFunctionDeclarationStatement statement);
		void Visit(ResolvedConstructorDeclarationStatement statement);
		void Visit(ResolvedDestructorDeclarationStatement statement);
		void Visit(ResolvedStringDeclarationStatement statement);
		void Visit(ResolvedOperatorDeclarationStatement statement);
		void Visit(ResolvedExpressionStatement statement);
		void Visit(ResolvedReturnStatement statement);
		void Visit(ResolvedBlockStatement statement);
		void Visit(ResolvedVarDeclarationStatement statement);
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
	public readonly ResolvedStatementNode body;

	public ResolvedFunctionDeclarationStatement(FunctionSymbol functionSymbol, ResolvedStatementNode body)
	{
		this.functionSymbol = functionSymbol;
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

public sealed class ResolvedExternalFunctionStatement : ResolvedStatementNode
{
	public readonly ExternalFunctionSymbol externalFunctionSymbol;

	public ResolvedExternalFunctionStatement(ExternalFunctionSymbol externalFunctionSymbol)
	{
		this.externalFunctionSymbol = externalFunctionSymbol;
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

public sealed class ResolvedEntryStatement : ResolvedStatementNode
{
	public readonly EntrySymbol? entrySymbol;
	public readonly ImmutableArray<ResolvedStatementNode> statements;
	
	public ResolvedEntryStatement(EntrySymbol entrySymbol, IEnumerable<ResolvedStatementNode> statements)
	{
		this.entrySymbol = entrySymbol;
		this.statements = statements.ToImmutableArray();
	}

	public ResolvedEntryStatement(ImmutableArray<ResolvedStatementNode> statements)
	{
		this.statements = statements;
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
	public readonly ResolvedStatementNode body;

	public ResolvedConstructorDeclarationStatement(ConstructorSymbol constructorSymbol, ResolvedStatementNode body)
	{
		this.constructorSymbol = constructorSymbol;
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

public sealed class ResolvedDestructorDeclarationStatement : ResolvedStatementNode
{
	public readonly DestructorSymbol destructorSymbol;
	public readonly ResolvedStatementNode body;
	
	public ResolvedDestructorDeclarationStatement(DestructorSymbol destructorSymbol, ResolvedStatementNode body)
	{
		this.destructorSymbol = destructorSymbol;
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

public sealed class ResolvedStringDeclarationStatement : ResolvedStatementNode
{
	public readonly StringFunctionSymbol stringFunctionSymbol;
	public readonly ResolvedStatementNode body;
	
	public ResolvedStringDeclarationStatement(StringFunctionSymbol stringFunctionSymbol, ResolvedStatementNode body)
	{
		this.stringFunctionSymbol = stringFunctionSymbol;
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

public sealed class ResolvedOperatorDeclarationStatement : ResolvedStatementNode
{
	public readonly OperatorOverloadSymbol operatorOverloadSymbol;
	public readonly ResolvedStatementNode body;

	public ResolvedOperatorDeclarationStatement(OperatorOverloadSymbol operatorOverloadSymbol,
		ResolvedStatementNode body)
	{
		this.operatorOverloadSymbol = operatorOverloadSymbol;
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

public sealed class ResolvedExpressionStatement : ResolvedStatementNode
{
	public readonly ResolvedExpressionNode value;
	
	public ResolvedExpressionStatement(ResolvedExpressionNode value)
	{
		this.value = value;
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

public sealed class ResolvedReturnStatement : ResolvedStatementNode
{
	public readonly ResolvedExpressionNode? value;
	
	public ResolvedReturnStatement(ResolvedExpressionNode? value)
	{
		this.value = value;
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

public sealed class ResolvedBlockStatement : ResolvedStatementNode
{
	public readonly ImmutableArray<ResolvedStatementNode> statements;
	
	public ResolvedBlockStatement(IEnumerable<ResolvedStatementNode> statements)
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

public sealed class ResolvedVarDeclarationStatement : ResolvedStatementNode
{
	public readonly VarSymbol varSymbol;
	public readonly ResolvedExpressionNode? initializer;
	
	public ResolvedVarDeclarationStatement(VarSymbol varSymbol, ResolvedExpressionNode? initializer)
	{
		this.varSymbol = varSymbol;
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