using Beanstalk.Analysis.Syntax;

namespace Beanstalk.Analysis.Semantics;

/// <summary>
/// Flattens an <see cref="Beanstalk.Analysis.Syntax.Ast"/> and resolves names and types
/// </summary>
public class Resolver : StatementNode.IVisitor<ResolvedStatementNode>, ExpressionNode.IVisitor<ResolvedExpressionNode>
{
	private readonly Collector collector;

	public Resolver(Collector collector)
	{
		this.collector = collector;
	}

	public ResolvedAst? Resolve(Ast ast)
	{
		return ast.Root switch
		{
			StatementNode statementNode => new ResolvedAst(statementNode.Accept(this)),
			ExpressionNode expressionNode => new ResolvedAst(expressionNode.Accept(this)),
			_ => null
		};
	}
	
	public ResolvedStatementNode Visit(ProgramStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ImportStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(DllImportStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ExternalFunctionStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ModuleStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(EntryStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(FunctionDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ConstructorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(DestructorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ExpressionStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(BlockStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(IfStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(MutableVarDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ImmutableVarDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ConstVarDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(ReturnStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(StructDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(InterfaceDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(CastDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(OperatorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(FieldDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedStatementNode Visit(DefineStatement statement)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(TokenExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(TupleExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(ListExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(MapExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(InstantiationExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(FunctionCallExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(CastExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(AccessExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(IndexExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(AssignmentExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(LambdaExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(ConditionalExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(BinaryExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(UnaryExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(SwitchExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(WithExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(BinaryOperationExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(UnaryOperationExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(PrimaryOperationExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(InterpolatedStringExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(TupleSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(GenericSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(MutableSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(ArraySyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(NullableSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(LambdaSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(ReferenceSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(BaseSyntaxType syntaxType)
	{
		throw new NotImplementedException();
	}
}