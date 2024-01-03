using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;
using FixedPointMath;

namespace Beanstalk.Analysis.Semantics;

public class ResolutionException : Exception
{
	public string WorkingDirectory { get; }
	public string FilePath { get; }
	
	public ResolutionException(string message, Token? token, string workingDirectory, string filePath)
		: base(FormatMessage(message, token))
	{
		WorkingDirectory = workingDirectory;
		FilePath = filePath;
	}

	public ResolutionException(string message, IBuffer source, TextRange range, string workingDirectory,
		string filePath) : base(FormatMessage(message, source, range))
	{
		WorkingDirectory = workingDirectory;
		FilePath = filePath;
	}

	private static string FormatMessage(string message, Token? token)
	{
		if (token is null)
			return message;

		return $"[line {token.Line}, column {token.Column} at '{token.Text}'] {message}";
	}

	private static string FormatMessage(string message, IBuffer source, TextRange range)
	{
		var (line, column) = source.GetLineColumn(range.Start);
		return $"[line {line}, column {column} at '{source.GetText(range)}'] {message}";
	}
}

/// <summary>
/// Flattens an <see cref="Beanstalk.Analysis.Syntax.Ast"/> and resolves names and types
/// </summary>
public class Resolver : CollectedStatementNode.IVisitor<ResolvedStatementNode>,
	ExpressionNode.IVisitor<ResolvedExpressionNode>, SyntaxType.IVisitor<Type>
{
	public readonly List<ResolutionException> exceptions = [];
	public SymbolTable? importedSymbols;
	private readonly Stack<Scope> scopeStack = new();
	private Scope CurrentScope => scopeStack.Peek();
	private IBuffer currentSource = StringBuffer.Empty;
	private string currentWorkingDirectory = "";
	private string currentFilePath = "";
	private readonly Dictionary<NativeSymbol, SymbolTable> nativeSymbolTables = new();

	public Resolver(Collector collector)
	{
		scopeStack.Push(collector.globalScope);
	}

	public ResolvedAst? Resolve(CollectedAst ast)
	{
		try
		{
			currentWorkingDirectory = ast.WorkingDirectory;
			currentFilePath = ast.FilePath;

			switch (ast.Root)
			{
				case CollectedStatementNode statementNode:
					return new ResolvedAst(statementNode.Accept(this), ast.WorkingDirectory, ast.FilePath);
				default:
					//CollectedExpressionNode expressionNode => new ResolvedAst(expressionNode.Accept(this)),
					return null;
			}
		}
		catch (ResolutionException e)
		{
			while (scopeStack.Count > 1)
				scopeStack.Pop();

			exceptions.Add(e);
			return null;
		}
		finally
		{
			currentWorkingDirectory = "";
			currentFilePath = "";
			currentSource = StringBuffer.Empty;
		}
	}

	public ResolvedStatementNode Visit(CollectedProgramStatement programStatement)
	{
		importedSymbols = programStatement.importedSymbols;
		var moduleSymbol = programStatement.moduleSymbol;

		if (moduleSymbol is not null)
			scopeStack.Push(moduleSymbol.Scope);

		var statements = new List<ResolvedStatementNode>();
		foreach (var statement in programStatement.topLevelStatements)
		{
			statements.Add(statement.Accept(this));
		}

		if (moduleSymbol is not null)
			scopeStack.Pop();

		importedSymbols = null;
		return new ResolvedProgramStatement(moduleSymbol, statements);
	}

	public ResolvedStatementNode Visit(CollectedModuleStatement moduleStatement)
	{
		scopeStack.Push(moduleStatement.moduleSymbol.Scope);

		var statements = new List<ResolvedStatementNode>();
		foreach (var statement in moduleStatement.topLevelStatements)
		{
			statements.Add(statement.Accept(this));
		}

		scopeStack.Pop();

		return new ResolvedModuleStatement(moduleStatement.moduleSymbol, statements);
	}

	public ResolvedStatementNode Visit(CollectedStructStatement structStatement)
	{
		scopeStack.Push(structStatement.structSymbol.Scope);

		var statements = new List<ResolvedStatementNode>();
		foreach (var statement in structStatement.statements)
		{
			statements.Add(statement.Accept(this));
		}

		scopeStack.Pop();

		return new ResolvedStructDeclarationStatement(structStatement.structSymbol, statements);
	}

	public ResolvedStatementNode Visit(CollectedFieldDeclarationStatement statement)
	{
		return new ResolvedFieldDeclarationStatement(statement.fieldSymbol, statement.initializer?.Accept(this));
	}

	public ResolvedStatementNode Visit(CollectedConstDeclarationStatement statement)
	{
		return new ResolvedConstDeclarationStatement(statement.constSymbol, statement.initializer.Accept(this));
	}

	public ResolvedStatementNode Visit(CollectedDefStatement statement)
	{
		return new ResolvedSimpleStatement(statement);
	}

	public ResolvedStatementNode Visit(CollectedFunctionDeclarationStatement statement)
	{
		return new ResolvedFunctionDeclarationStatement(statement.functionSymbol!);
	}

	public ResolvedStatementNode Visit(CollectedConstructorDeclarationStatement statement)
	{
		return new ResolvedConstructorDeclarationStatement(statement.constructorSymbol!);
	}

	public ResolvedStatementNode Visit(CollectedDestructorDeclarationStatement statement)
	{
		return new ResolvedDestructorDeclarationStatement(statement.destructorSymbol!);
	}

	public ResolvedStatementNode Visit(CollectedCastDeclarationStatement statement)
	{
		// Todo
		return new ResolvedSimpleStatement(statement);
	}

	public ResolvedStatementNode Visit(CollectedOperatorDeclarationStatement statement)
	{
		// Todo
		return new ResolvedSimpleStatement(statement);
	}

	public ResolvedStatementNode Visit(CollectedSimpleStatement statement)
	{
		// Todo
		return new ResolvedSimpleStatement(statement);
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
		if (expression.caller.Accept(this) is not { } caller)
			throw NewResolutionException($"Function '{expression.caller}' not found", expression.caller.range);

		var arguments = new List<ResolvedExpressionNode>();
		foreach (var argument in expression.arguments)
		{
			arguments.Add(argument.Accept(this));
		}

		switch (caller)
		{
			case ResolvedFunctionExpression functionExpression:
				return new ResolvedFunctionCallExpression(functionExpression.functionSymbol, arguments);
			default:
				throw NewResolutionException("Invalid function call target", expression.caller.range);
		}
	}

	public ResolvedExpressionNode Visit(CastExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(AccessExpression expression)
	{
		/*Type source;
		var staticAccess = false;

		switch (expression.source)
		{
			case TokenExpression tokenExpression:
				if (!IsValidAccessTarget(tokenExpression.token.Type))
					throw NewResolutionException($"Invalid access target '{tokenExpression.token.Text}'",
						tokenExpression.token);

				var sourceExpression = tokenExpression.Accept(this);
				if (sourceExpression is ResolvedTypeExpression)
					staticAccess = true;

				if (sourceExpression.Type is not { } sourceType)
					throw NewResolutionException("Unable to determining access source's type", tokenExpression.token);
				
				source = sourceExpression.Type;
				break;
			
			case SyntaxType syntaxType:
				staticAccess = true;
				source = syntaxType.Accept(this);
				break;
			
			default:
				throw NewResolutionException("Invalid access target", expression.range);
		}

		SymbolTable sourceTable;
		switch (source)
		{
			case BaseType sourceType:
				sourceTable = sourceType.typeSymbol;
				break;
		}

		var targetSymbol = CurrentScope.LookupSymbol(expression.target.Text);
		if (targetSymbol is null)
			throw NewResolutionException($"Could not find a symbol named '{expression.target.Text}'",
				expression.target);

		switch (targetSymbol)
		{
			case FunctionSymbol functionSymbol:
				return new ResolvedSymbolExpression(targetSymbol, functionSymbol.GetFunctionType());
			default:
				return new ResolvedSymbolExpression(targetSymbol);
		}*/
		throw new NotImplementedException();
	}

	private bool IsValidAccessTarget(TokenType tokenType)
	{
		return TokenType.NativeDataTypes.Contains(tokenType) || tokenType == TokenType.Identifier;
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

	public ResolvedExpressionNode Visit(TokenExpression expression)
	{
		var token = expression.token;
		var tokenType = token.Type;
		
		Type? type = null;
		if (tokenType == TokenType.CharLiteral)
		{
			type = new BaseType(TypeSymbol.Char);
		}
		else if (tokenType == TokenType.KeywordTrue || tokenType == TokenType.KeywordFalse)
		{
			type = new BaseType(TypeSymbol.Bool);
		}
		else if (tokenType == TokenType.StringLiteral || tokenType == TokenType.InterpolatedStringLiteral)
		{
			type = new BaseType(TypeSymbol.String);
		}
		else if (tokenType == TokenType.NumberLiteral)
		{
			type = token.Value switch
			{
				sbyte => new BaseType(TypeSymbol.Int8),
				byte => new BaseType(TypeSymbol.UInt8),
				short => new BaseType(TypeSymbol.Int16),
				ushort => new BaseType(TypeSymbol.UInt16),
				int => new BaseType(TypeSymbol.Int32),
				uint => new BaseType(TypeSymbol.UInt32),
				long => new BaseType(TypeSymbol.Int64),
				ulong => new BaseType(TypeSymbol.UInt64),
				Int128 => new BaseType(TypeSymbol.Int128),
				UInt128 => new BaseType(TypeSymbol.UInt128),
				float => new BaseType(TypeSymbol.Float32),
				double => new BaseType(TypeSymbol.Float64),
				decimal => new BaseType(TypeSymbol.Float128),
				Fixed32 => new BaseType(TypeSymbol.Fixed32),
				Fixed64 => new BaseType(TypeSymbol.Fixed64),
				Fixed128 => new BaseType(TypeSymbol.Fixed128),
				_ => null
			};
		}
		else if (tokenType == TokenType.Identifier)
		{
			var symbol = CurrentScope.LookupSymbol(token.Text);

			if (symbol is null)
				throw NewResolutionException($"Unable to resolve symbol '{token.Text}'", token);

			return symbol switch
			{
				FieldSymbol fieldSymbol => new ResolvedFieldExpression(fieldSymbol),
				ConstSymbol constSymbol => new ResolvedConstExpression(constSymbol),
				FunctionSymbol functionSymbol => new ResolvedFunctionExpression(functionSymbol),
				VarSymbol varSymbol => new ResolvedVarExpression(varSymbol),
				TypeSymbol typeSymbol => new ResolvedTypeExpression(typeSymbol),
				_ => throw NewResolutionException($"Unknown symbol class for symbol '{token.Text}'", token)
			};
		}

		return new ResolvedLiteralExpression(token, type);
	}

	private TypeSymbol FindType(Token token)
	{
		if (token.Type == TokenType.KeywordInt)
			return TypeSymbol.Int.LinkedSymbol as TypeSymbol ??
			       throw NewResolutionException("Invalid operation: Int is linked incorrectly", token);
		if (token.Type == TokenType.KeywordInt8)
			return TypeSymbol.Int8;
		if (token.Type == TokenType.KeywordInt16)
			return TypeSymbol.Int16;
		if (token.Type == TokenType.KeywordInt32)
			return TypeSymbol.Int32;
		if (token.Type == TokenType.KeywordInt64)
			return TypeSymbol.Int64;
		if (token.Type == TokenType.KeywordInt128)
			return TypeSymbol.Int128;


		if (token.Type == TokenType.KeywordUInt)
			return TypeSymbol.UInt.LinkedSymbol as TypeSymbol ??
			       throw NewResolutionException("Invalid operation: UInt is linked incorrectly", token);
		if (token.Type == TokenType.KeywordUInt8)
			return TypeSymbol.UInt8;
		if (token.Type == TokenType.KeywordUInt16)
			return TypeSymbol.UInt16;
		if (token.Type == TokenType.KeywordUInt32)
			return TypeSymbol.UInt32;
		if (token.Type == TokenType.KeywordUInt64)
			return TypeSymbol.UInt64;
		if (token.Type == TokenType.KeywordUInt128)
			return TypeSymbol.UInt128;

		if (token.Type == TokenType.KeywordFloat)
			return TypeSymbol.Float.LinkedSymbol as TypeSymbol ??
			       throw NewResolutionException("Invalid operation: Float is linked incorrectly", token);
		if (token.Type == TokenType.KeywordFloat32)
			return TypeSymbol.Float32;
		if (token.Type == TokenType.KeywordFloat64)
			return TypeSymbol.Float64;
		if (token.Type == TokenType.KeywordFloat128)
			return TypeSymbol.Float128;

		if (token.Type == TokenType.KeywordFixed)
			return TypeSymbol.Fixed.LinkedSymbol as TypeSymbol ??
			       throw NewResolutionException("Invalid operation: Fixed is linked incorrectly", token);
		if (token.Type == TokenType.KeywordFixed32)
			return TypeSymbol.Fixed32;
		if (token.Type == TokenType.KeywordFixed64)
			return TypeSymbol.Fixed64;
		if (token.Type == TokenType.KeywordFixed128)
			return TypeSymbol.Fixed128;

		if (token.Type == TokenType.KeywordBool)
			return TypeSymbol.Bool;
		if (token.Type == TokenType.KeywordChar)
			return TypeSymbol.Char;
		if (token.Type == TokenType.KeywordString)
			return TypeSymbol.String;

		if (token.Type != TokenType.Identifier && token.Type != TokenType.KeywordNInt &&
		    token.Type != TokenType.KeywordNUInt)
			throw NewResolutionException($"Token '{token.Text}' is not a valid type", token);
		
		var symbol = CurrentScope.LookupSymbol(token.Text);
		if (symbol is not TypeSymbol typeSymbol)
			throw NewResolutionException($"Symbol '{token.Text}' is not a valid type", token);
			
		return typeSymbol;

	}

	private ResolutionException NewResolutionException(string message, Token? token)
	{
		return new ResolutionException(message, token, currentWorkingDirectory, currentFilePath);
	}

	private ResolutionException NewResolutionException(string message, TextRange range)
	{
		return new ResolutionException(message, currentSource, range, currentWorkingDirectory, currentFilePath);
	}

	public Type Visit(TupleSyntaxType syntaxType)
	{
		var types = new List<Type>();

		foreach (var type in syntaxType.types)
		{
			types.Add(type.Accept(this));
		}

		return new TupleType(types);
	}

	public Type Visit(GenericSyntaxType syntaxType)
	{
		var typeParameters = new List<Type>();

		foreach (var typeParameter in syntaxType.typeParameters)
		{
			typeParameters.Add(typeParameter.Accept(this));
		}

		return new GenericType(syntaxType.baseSyntaxType.Accept(this), typeParameters);
	}

	public Type Visit(MutableSyntaxType syntaxType)
	{
		return new MutableType(syntaxType.baseSyntaxType.Accept(this));
	}

	public Type Visit(ArraySyntaxType syntaxType)
	{
		return new ArrayType(syntaxType.baseSyntaxType.Accept(this));
	}

	public Type Visit(NullableSyntaxType syntaxType)
	{
		return new NullableType(syntaxType.baseSyntaxType.Accept(this));
	}

	public Type Visit(LambdaSyntaxType syntaxType)
	{
		var parameterTypes = new List<Type>();

		foreach (var typeParameter in syntaxType.parameterTypes)
		{
			parameterTypes.Add(typeParameter.Accept(this));
		}

		return new FunctionType(parameterTypes, syntaxType.returnType?.Accept(this));
	}

	public Type Visit(ReferenceSyntaxType syntaxType)
	{
		return new ReferenceType(syntaxType.baseSyntaxType.Accept(this), syntaxType.immutable);
	}

	public Type Visit(BaseSyntaxType syntaxType)
	{
		return new BaseType(FindType(syntaxType.token));
	}

	private void PopulateNativeSymbolTables()
	{
		nativeSymbolTables.Add(TypeSymbol.Int8, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Int16, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Int32, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Int64, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Int128, new SymbolTable());
		
		nativeSymbolTables.Add(TypeSymbol.UInt8, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.UInt16, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.UInt32, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.UInt64, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.UInt128, new SymbolTable());
		
		nativeSymbolTables.Add(TypeSymbol.Float32, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Float64, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Float128, new SymbolTable());
		
		nativeSymbolTables.Add(TypeSymbol.Fixed32, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Fixed64, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Fixed128, new SymbolTable());
		
		nativeSymbolTables.Add(TypeSymbol.Bool, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.Char, new SymbolTable());
		nativeSymbolTables.Add(TypeSymbol.String, new SymbolTable());
	}
	
	private ISymbol? LookupSymbolWithImports(string name)
	{
		if (CurrentScope.LookupSymbol(name) is { } symbol)
		{
			if (symbol is AliasedSymbol alias)
				return alias.LinkedSymbol;
			
			return symbol;
		}

		var importedSymbol = importedSymbols!.Lookup(name);
		if (importedSymbol is AliasedSymbol aliasedImport)
			return aliasedImport.LinkedSymbol;

		return importedSymbol;
	}
}