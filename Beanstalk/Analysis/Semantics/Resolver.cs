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
	private readonly Stack<TypeSymbol> typeStack = new();
	private TypeSymbol CurrentType => typeStack.Peek();
	private readonly Stack<IFunctionSymbol> functionStack = new();
	private IFunctionSymbol CurrentFunction => functionStack.Peek();
	private bool StaticContext => !functionStack.TryPeek(out var currentFunction) || currentFunction.IsStatic;
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
			currentSource = ast.Source;

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
			try
			{
				statements.Add(statement.Accept(this));
			}
			catch (ResolutionException e)
			{
				exceptions.Add(e);
			}
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
			try
			{
				statements.Add(statement.Accept(this));
			}
			catch (ResolutionException e)
			{
				exceptions.Add(e);
			}
		}

		scopeStack.Pop();

		return new ResolvedModuleStatement(moduleStatement.moduleSymbol, statements);
	}

	public ResolvedStatementNode Visit(CollectedStructDeclarationStatement structDeclarationStatement)
	{
		scopeStack.Push(structDeclarationStatement.structSymbol.Scope);
		typeStack.Push(structDeclarationStatement.structSymbol);

		var statements = new List<ResolvedStatementNode>();
		foreach (var statement in structDeclarationStatement.statements)
		{
			try
			{
				statements.Add(statement.Accept(this));
			}
			catch (ResolutionException e)
			{
				exceptions.Add(e);
			}
		}

		typeStack.Pop();
		scopeStack.Pop();

		return new ResolvedStructDeclarationStatement(structDeclarationStatement.structSymbol, statements);
	}

	public ResolvedStatementNode Visit(CollectedFieldDeclarationStatement statement)
	{
		if (!CurrentType.IsMutable && statement.fieldSymbol.IsMutable)
			throw NewResolutionException(
				$"Cannot declare field '{statement.fieldSymbol.Name}' as mutable because the parent type " +
				$"'{CurrentType.Name}' is immutable", statement.range);

		var initializer = statement.initializer?.Accept(this);
		statement.fieldSymbol.Initializer = initializer;

		if (statement.fieldSymbol.IsStatic)
			CurrentType.HasStaticFields = true;
		
		return new ResolvedFieldDeclarationStatement(statement.fieldSymbol, initializer);
	}

	public ResolvedStatementNode Visit(CollectedConstDeclarationStatement statement)
	{
		var initializer = statement.initializer.Accept(this);

		if (!initializer.IsConstant)
			throw NewResolutionException("Constant initializer must be a compile-time constant expression",
				statement.initializer.range);
		
		return new ResolvedConstDeclarationStatement(statement.constSymbol, initializer);
	}

	public ResolvedStatementNode Visit(CollectedDefStatement statement)
	{
		return new ResolvedSimpleStatement(statement);
	}

	public ResolvedStatementNode Visit(CollectedEntryStatement entryStatement)
	{
		scopeStack.Push(entryStatement.entrySymbol!.Body);
		functionStack.Push(entryStatement.entrySymbol!);
		var statements = new List<ResolvedStatementNode>();
		foreach (var statement in entryStatement.statements)
		{
			try
			{
				statements.Add(statement.Accept(this));
			}
			catch (ResolutionException e)
			{
				exceptions.Add(e);
			}
		}
		functionStack.Pop();
		scopeStack.Pop();
		
		return new ResolvedEntryStatement(entryStatement.entrySymbol!, statements);
	}

	public ResolvedStatementNode Visit(CollectedFunctionDeclarationStatement functionDeclarationStatement)
	{
		scopeStack.Push(functionDeclarationStatement.functionSymbol!.Body);
		functionStack.Push(functionDeclarationStatement.functionSymbol!);
		var body = functionDeclarationStatement.body.Accept(this);
		functionStack.Pop();
		scopeStack.Pop();
		
		return new ResolvedFunctionDeclarationStatement(functionDeclarationStatement.functionSymbol!, body);
	}

	public ResolvedStatementNode Visit(CollectedExternalFunctionStatement externalFunctionStatement)
	{
		return new ResolvedExternalFunctionStatement(externalFunctionStatement.externalFunctionSymbol!);
	}

	public ResolvedStatementNode Visit(CollectedConstructorDeclarationStatement statement)
	{
		scopeStack.Push(statement.scope);
		functionStack.Push(statement.constructorSymbol!);
		var body = statement.body.Accept(this);
		functionStack.Pop();
		scopeStack.Pop();
		
		return new ResolvedConstructorDeclarationStatement(statement.constructorSymbol!, body);
	}

	public ResolvedStatementNode Visit(CollectedDestructorDeclarationStatement statement)
	{
		scopeStack.Push(statement.scope);
		functionStack.Push(statement.destructorSymbol!);
		var body = statement.body.Accept(this);
		functionStack.Pop();
		scopeStack.Pop();
		
		return new ResolvedDestructorDeclarationStatement(statement.destructorSymbol!, body);
	}

	public ResolvedStatementNode Visit(CollectedStringDeclarationStatement statement)
	{
		scopeStack.Push(statement.scope);
		functionStack.Push(statement.stringFunctionSymbol!);
		var body = statement.body.Accept(this);
		functionStack.Pop();
		scopeStack.Pop();
		
		return new ResolvedStringDeclarationStatement(statement.stringFunctionSymbol!, body);
	}

	public ResolvedStatementNode Visit(CollectedCastDeclarationStatement statement)
	{
		// Todo
		return new ResolvedSimpleStatement(statement);
	}

	public ResolvedStatementNode Visit(CollectedOperatorDeclarationStatement statement)
	{
		scopeStack.Push(statement.operatorOverloadSymbol!.Body);
		functionStack.Push(statement.operatorOverloadSymbol!);
		var body = statement.body.Accept(this);
		functionStack.Pop();
		scopeStack.Pop();
		
		return new ResolvedOperatorDeclarationStatement(statement.operatorOverloadSymbol!, body);
	}

	public ResolvedStatementNode Visit(CollectedExpressionStatement statement)
	{
		return new ResolvedExpressionStatement(statement.statement.expression.Accept(this));
	}

	public ResolvedStatementNode Visit(CollectedBlockStatement statement)
	{
		scopeStack.Push(statement.scope);
		
		var statements = new List<ResolvedStatementNode>();
		foreach (var bodyStatement in statement.statements)
		{
			statements.Add(bodyStatement.Accept(this));
		}

		scopeStack.Pop();
		return new ResolvedBlockStatement(statements);
	}

	public ResolvedStatementNode Visit(CollectedVarDeclarationStatement statement)
	{
		var type = statement.syntaxType?.Accept(this);
		var initializer = statement.initializer?.Accept(this);

		if (type is null)
		{
			if (initializer is null)
				throw NewResolutionException($"Type of variable '{statement.varSymbol.Name}' cannot be inferred",
					statement.varToken);

			type = initializer.Type;
		}

		statement.varSymbol.EvaluatedType = type;

		return new ResolvedVarDeclarationStatement(statement.varSymbol, initializer);
	}

	public ResolvedStatementNode Visit(CollectedSimpleStatement statement)
	{
		switch (statement.statementNode)
		{
			case ReturnStatement statementNode:
				return new ResolvedReturnStatement(statementNode.expression?.Accept(this));
		}
		
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
			case ResolvedFunctionSymbolExpression functionExpression:
				return new ResolvedFunctionCallExpression(functionExpression.functionSymbol, arguments);
			
			case ResolvedExternalFunctionSymbolExpression functionExpression:
				return new ResolvedExternalFunctionCallExpression(functionExpression.functionSymbol, arguments);
			
			case ResolvedTypeAccessExpression typeAccessExpression:
				switch (typeAccessExpression.target)
				{
					case FunctionSymbol functionSymbol:
						return new ResolvedFunctionCallExpression(functionSymbol, arguments);
					
					case ConstructorSymbol constructorSymbol:
						return new ResolvedConstructorCallExpression(constructorSymbol, arguments);
					
					default:
						throw NewResolutionException("Invalid static method call target", expression.caller.range);
				}
				
			case ResolvedValueAccessExpression valueAccessExpression:
				switch (valueAccessExpression.target)
				{
					case FunctionSymbol functionSymbol:
						return new ResolvedFunctionCallExpression(functionSymbol, arguments);
					
					case StringFunctionSymbol stringFunctionSymbol:
						return new ResolvedStringCallExpression(stringFunctionSymbol, valueAccessExpression.source);
					
					default:
						throw NewResolutionException("Invalid method call target", expression.caller.range);
				}
			
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
		var staticAccess = false;

		switch (expression.source)
		{
			case TokenExpression tokenExpression:
			{
				if (!IsTokenValidAccessTarget(tokenExpression.token.Type))
					throw NewResolutionException($"Invalid access target '{tokenExpression.token.Text}'",
						tokenExpression.token);

				var sourceExpression = tokenExpression.Accept(this);
				if (sourceExpression is ResolvedTypeSymbolExpression)
					staticAccess = true;

				// Todo: Support native types, array types, nullable types, etc.

				return ResolveAccess(sourceExpression, expression.source.range, expression.target, staticAccess);
			}

			case AccessExpression sourceAccessExpression:
			{
				// Todo
				var sourceExpression = (ResolvedValueAccessExpression)sourceAccessExpression.Accept(this);
				var source = sourceExpression.source;
				break;
			}

			case BinaryExpression sourceBinaryExpression:
			{
				if (sourceBinaryExpression.Accept(this) is not ResolvedBinaryExpression binaryExpression)
					throw NewResolutionException("Invalid access target", expression.range);

				return ResolveAccess(binaryExpression, sourceBinaryExpression.range, expression.target, false);
			}

			case FunctionCallExpression functionCallExpression:
			{
				switch (functionCallExpression.Accept(this))
				{
					// Todo: Support all function call types
					default:
						throw NewResolutionException("Invalid access target", expression.range);

					case ResolvedConstructorCallExpression callExpression:
						return ResolveAccess(callExpression, functionCallExpression.range, expression.target, false);
				}
			}

			case SyntaxType syntaxType:
			{
				// Todo
				throw new NotImplementedException();
				staticAccess = true;
				var source = syntaxType.Accept(this);
				break;
			}

			default:
				throw NewResolutionException("Invalid access target", expression.range);
		}
		
		throw new NotImplementedException();
	}

	private ResolvedExpressionNode ResolveAccess(ResolvedExpressionNode sourceExpression, TextRange sourceRange,
		Token target, bool staticAccess)
	{
		if (sourceExpression.Type is not BaseType sourceType)
			throw NewResolutionException("Unable to determine access source's type", sourceRange);

		switch (sourceType.typeSymbol)
		{
			case StructSymbol structSymbol:
			{
				var targetSymbol = structSymbol.Scope.SymbolTable.Lookup(
					target.Type == TokenType.KeywordNew
						? ConstructorSymbol.InternalName
						: target.Type == TokenType.KeywordString
							? StringFunctionSymbol.InternalName
							: target.Text);

				if (targetSymbol is null)
					throw NewResolutionException($"Symbol '{target.Text}' not found",
						target);
				
				if (staticAccess)
				{
					var cannotBeStaticException = NewResolutionException(
						$"Symbol '{targetSymbol.Name}' cannot be accessed from a static context", target);
					
					switch (targetSymbol)
					{
						case ConstSymbol symbol:
							if (!symbol.IsStatic)
								throw cannotBeStaticException;
							
							break;
						
						case FieldSymbol symbol:
							if (!symbol.IsStatic)
								throw cannotBeStaticException;
							
							break;
						
						case StringFunctionSymbol symbol:
							if (!symbol.IsStatic)
								throw cannotBeStaticException;
							
							break;
						
						// Todo: Implement once static functions are added
						/*case FunctionSymbol symbol:
							if (!symbol.IsStatic)
								throw cannotBeStatic;
							
							break;*/
					}
					
					return new ResolvedTypeAccessExpression(sourceType, targetSymbol);
				}

				var mustBeStaticException = NewResolutionException(
					$"Symbol '{targetSymbol.Name}' must be accessed from a static context. " +
					$"Did you mean '{structSymbol.Name}.{target.Text}'?", target);
				
				switch (targetSymbol)
				{
					case ConstSymbol symbol:
						if (symbol.IsStatic)
							throw mustBeStaticException;
						
						break;
					
					case FieldSymbol symbol:
						if (symbol.IsStatic)
							throw mustBeStaticException;
						
						break;
					
					case StringFunctionSymbol symbol:
						if (symbol.IsStatic)
							throw mustBeStaticException;
						
						break;
					
					// Todo: Implement once static functions are added
					/*case FunctionSymbol symbol:
						if (!symbol.IsStatic)
							throw mustBeStaticException;
						
						break;*/
					
					default:
						throw mustBeStaticException;
				}
				
				return new ResolvedValueAccessExpression(sourceExpression, targetSymbol);
			}
		}
		
		throw NewResolutionException("Invalid access target", sourceRange);
	}

	private bool IsTokenValidAccessTarget(TokenType tokenType)
	{
		return TokenType.NativeDataTypes.Contains(tokenType) || tokenType == TokenType.Identifier ||
		       tokenType == TokenType.KeywordThis;
	}

	public ResolvedExpressionNode Visit(IndexExpression expression)
	{
		throw new NotImplementedException();
	}

	public ResolvedExpressionNode Visit(AssignmentExpression expression)
	{
		// Todo: Verify 'left' is a valid assignment target
		var left = expression.left.Accept(this);
		var right = expression.right.Accept(this);

		switch (left)
		{
			case ResolvedTypeAccessExpression leftExpression:
			{
				switch (leftExpression.target)
				{
					case FieldSymbol fieldSymbol:
						if (!fieldSymbol.IsMutable)
						{
							if (CurrentFunction is not ConstructorSymbol)
							{
								throw NewResolutionException(
									"Immutable fields cannot be modified outside of a constructor",
									expression.left.range);
							}
						}
						break;
					
					default:
						throw NewResolutionException(
							$"Symbol '{leftExpression.target.Name}' is not a valid assignment target",
							expression.left.range);
				}
				break;
			}
			
			case ResolvedValueAccessExpression leftExpression:
			{
				switch (leftExpression.target)
				{
					case FieldSymbol fieldSymbol:
						if (!fieldSymbol.IsMutable)
						{
							if (CurrentFunction is not ConstructorSymbol)
							{
								throw NewResolutionException(
									"Immutable fields cannot be modified outside of a constructor",
									expression.left.range);
							}
						}
						break;
					
					default:
						throw NewResolutionException(
							$"Symbol '{leftExpression.target.Name}' is not a valid assignment target",
							expression.left.range);
				}
				break;
			}
			
			case ResolvedFieldExpression leftExpression:
			{
				if (!leftExpression.fieldSymbol.IsMutable)
				{
					if (CurrentFunction is not ConstructorSymbol)
					{
						throw NewResolutionException(
							"Immutable fields cannot be modified outside of a constructor",
							expression.left.range);
					}
				}
				break;
			}
			
			case ResolvedVarExpression leftExpression:
			{
				if (!leftExpression.varSymbol.IsMutable)
				{
					throw NewResolutionException(
						"Immutable variables cannot be reassigned",
						expression.left.range);
				}
				break;
			}
			
			case ResolvedConstExpression:
				throw NewResolutionException("Constants cannot be reassigned", expression.left.range);
			
			case ResolvedFunctionSymbolExpression leftExpression:
				throw NewResolutionException($"Symbol '{leftExpression.functionSymbol.Name}' " +
				                             $"is not a valid assignment target", expression.left.range);
			
			case ResolvedExternalFunctionSymbolExpression leftExpression:
				throw NewResolutionException($"Symbol '{leftExpression.functionSymbol.Name}' " +
				                             $"is not a valid assignment target", expression.left.range);
			
			case ResolvedConstructorSymbolExpression:
				throw NewResolutionException("Constructors are not a valid assignment target", expression.left.range);
			
			case ResolvedStringFunctionSymbolExpression:
				throw NewResolutionException("String functions are not a valid assignment target",
					expression.left.range);
					
			default:
				throw NewResolutionException(
					"Invalid assignment target", expression.left.range);
		}
		
		// Todo: Type check with implicit casts if needed
		return new ResolvedAssignmentExpression(left, right);
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
		var left = expression.left.Accept(this);
		var right = expression.right.Accept(this);
		
		// Todo: Handle 1-deep implicit casts

		var operatorSymbol = Type.FindOperator(left.Type, right.Type, expression.operation);
		if (operatorSymbol is null)
			throw NewResolutionException($"Cannot apply operator '{expression.op.Text}' for operands of type " +
			                             $"'{left.Type?.ToString() ?? "null"}' and " +
			                             $"'{right.Type?.ToString() ?? "null"}'", expression.op);

		if (operatorSymbol.hasError)
			throw NewResolutionException(operatorSymbol.error!, expression.op);
		
		return new ResolvedBinaryExpression(left, right, operatorSymbol.result!, expression.operation);
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
			var symbol = LookupSymbolWithImports(token.Text);

			if (symbol is null)
				throw NewResolutionException($"Unable to resolve symbol '{token.Text}'", token);

			switch (symbol)
			{
				case FieldSymbol fieldSymbol:
					if (StaticContext && !fieldSymbol.IsStatic)
						throw NewResolutionException($"Cannot access '{token.Text}' from a static context", token);

					if (fieldSymbol.IsStatic)
					{
						return new ResolvedFieldExpression(fieldSymbol);
					}
					
					var thisExpression = new ResolvedThisExpression(CurrentType.EvaluatedType);
					return new ResolvedValueAccessExpression(thisExpression, fieldSymbol);
				
				case ConstSymbol constSymbol:
					if (StaticContext && !constSymbol.IsStatic)
						throw NewResolutionException($"Cannot access '{token.Text}' from a static context", token);

					return new ResolvedConstExpression(constSymbol);
				
				case FunctionSymbol functionSymbol:
					if (StaticContext && !functionSymbol.IsStatic)
						throw NewResolutionException($"Cannot access '{token.Text}' from a static context", token);

					return new ResolvedFunctionSymbolExpression(functionSymbol);
				
				case StringFunctionSymbol stringFunctionSymbol:
					if (StaticContext && !stringFunctionSymbol.IsStatic)
						throw NewResolutionException($"Cannot access '{token.Text}' from a static context", token);

					return new ResolvedStringFunctionSymbolExpression(stringFunctionSymbol);
				
				case ExternalFunctionSymbol functionSymbol:
					return new ResolvedExternalFunctionSymbolExpression(functionSymbol);
				
				case VarSymbol varSymbol:
					return new ResolvedVarExpression(varSymbol);
				
				case ParameterSymbol parameterSymbol:
					return new ResolvedParameterExpression(parameterSymbol);
				
				case TypeSymbol typeSymbol:
					return new ResolvedTypeSymbolExpression(typeSymbol);
				
				default:
					throw NewResolutionException($"Unknown symbol class for symbol '{token.Text}'", token);
			}
		}
		else if (tokenType == TokenType.KeywordThis)
		{
			if (StaticContext)
				throw NewResolutionException("'this' is not valid within a static context", token);
			
			return new ResolvedThisExpression(CurrentType.EvaluatedType);
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
		
		var symbol = LookupSymbolWithImports(token.Text);
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