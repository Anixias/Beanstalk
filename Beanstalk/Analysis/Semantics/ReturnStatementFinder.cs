using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

/// <summary>
/// Visits an <see cref="Ast"/> to determine if a value is returned by a block
/// </summary>
public sealed class ReturnStatementFinder : StatementNode.IVisitor
{
	private sealed class ReturnedExpressionFound : Exception
	{
		
	}

	private ReturnStatementFinder()
	{
		
	}

	public static bool Find(StatementNode statement)
	{
		var finder = new ReturnStatementFinder();
		return finder.FindInternal(statement);
	}

	private bool FindInternal(StatementNode statement)
	{
		try
		{
			statement.Accept(this);
			return false;
		}
		catch (ReturnedExpressionFound)
		{
			return true;
		}
	}
	
	public void Visit(ProgramStatement programStatement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ImportStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(DllImportStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ExternalFunctionStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ModuleStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(EntryStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(FunctionDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ConstructorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(DestructorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ExpressionStatement statement)
	{
		// Do nothing
	}

	public void Visit(BlockStatement blockStatement)
	{
		foreach (var statement in blockStatement.statements)
			statement.Accept(this);
	}

	public void Visit(IfStatement statement)
	{
		statement.thenBranch.Accept(this);
		statement.elseBranch?.Accept(this);
	}

	public void Visit(MutableVarDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ImmutableVarDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ConstVarDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ReturnStatement statement)
	{
		if (statement.expression is not null)
			throw new ReturnedExpressionFound();
	}

	public void Visit(StructDeclarationStatement structDeclarationStatement)
	{
		throw new NotImplementedException();
	}

	public void Visit(InterfaceDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(CastDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(OperatorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(FieldDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(DefineStatement statement)
	{
		throw new NotImplementedException();
	}
}