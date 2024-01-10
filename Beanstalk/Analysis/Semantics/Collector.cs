using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public class CollectionException : Exception
{
	public string WorkingDirectory { get; }
	public string FilePath { get; }
	public CollectionException(string message, Token? token, string workingDirectory, string filePath)
		: base(FormatMessage(message, token))
	{
		WorkingDirectory = workingDirectory;
		FilePath = filePath;
	}

	public CollectionException(string message, IBuffer source, TextRange range, string workingDirectory,
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
/// Collects symbols and scopes from a list of <see cref="Beanstalk.Analysis.Syntax.Ast"/> instances
/// </summary>
public partial class Collector : StatementNode.IVisitor<CollectedStatementNode>
{
	public readonly Scope globalScope = new();
	
	private readonly Stack<Scope> scopeStack = new();
	private Scope CurrentScope => scopeStack.Peek();
	private readonly Stack<TypeSymbol> typeStack = new();
	private TypeSymbol CurrentType => typeStack.Peek();
	
	public readonly List<CollectionException> exceptions = [];
	private string currentWorkingDirectory = "";
	private string currentFilePath = "";
	private IBuffer? currentSource;

	public Collector(bool is64Bit)
	{
		scopeStack.Push(globalScope);
		AddNativeTypes(globalScope, is64Bit);
	}

	private static void AddNativeTypes(Scope scope, bool is64Bit)
	{
		scope.SymbolTable.Add(TypeSymbol.Int8);
		scope.SymbolTable.Add(TypeSymbol.UInt8);
		scope.SymbolTable.Add(TypeSymbol.Int16);
		scope.SymbolTable.Add(TypeSymbol.UInt16);
		scope.SymbolTable.Add(TypeSymbol.Int32);
		scope.SymbolTable.Add(TypeSymbol.UInt32);
		scope.SymbolTable.Add(TypeSymbol.Int64);
		scope.SymbolTable.Add(TypeSymbol.UInt64);
		scope.SymbolTable.Add(TypeSymbol.Int128);
		scope.SymbolTable.Add(TypeSymbol.UInt128);
		scope.SymbolTable.Add(TypeSymbol.Int);
		scope.SymbolTable.Add(TypeSymbol.UInt);
		scope.SymbolTable.Add(TypeSymbol.Float32);
		scope.SymbolTable.Add(TypeSymbol.Float64);
		scope.SymbolTable.Add(TypeSymbol.Float128);
		scope.SymbolTable.Add(TypeSymbol.Float);
		scope.SymbolTable.Add(TypeSymbol.Fixed32);
		scope.SymbolTable.Add(TypeSymbol.Fixed64);
		scope.SymbolTable.Add(TypeSymbol.Fixed128);
		scope.SymbolTable.Add(TypeSymbol.Fixed);
		scope.SymbolTable.Add(TypeSymbol.Char);
		scope.SymbolTable.Add(TypeSymbol.String);
		scope.SymbolTable.Add(TypeSymbol.Bool);

		var nint = new AliasedSymbol(TokenType.KeywordNInt.ToString(), is64Bit
			? TypeSymbol.Int64
			: TypeSymbol.Int32);
		
		var nuint = new AliasedSymbol(TokenType.KeywordNUInt.ToString(), is64Bit
			? TypeSymbol.UInt64
			: TypeSymbol.UInt32);
		
		scope.SymbolTable.Add(nint);
		scope.SymbolTable.Add(nuint);
	}

	private CollectionException NewCollectionException(string message, Token? token = null)
	{
		return new CollectionException(message, token, currentWorkingDirectory, currentFilePath);
	}

	private CollectionException NewCollectionException(string message, TextRange range)
	{
		return new CollectionException(message, currentSource!, range, currentWorkingDirectory, currentFilePath);
	}

	public CollectedAst? Collect(Ast ast, string workingDirectory, string filePath)
	{
		try
		{
			currentWorkingDirectory = workingDirectory;
			currentFilePath = filePath;
			currentSource = ast.Source;

			var root = ast.Root switch
			{
				StatementNode statementNode => statementNode.Accept(this),
				_ => null
			};

			if (scopeStack.Count != 1)
				throw NewCollectionException("Invalid operation: Scope stack unbalanced");

			if (root is null)
				return null;

			return new CollectedAst(root, ast.Source, workingDirectory, filePath);
		}
		catch (CollectionException e)
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
			currentSource = null;
		}
	}
	
	public CollectedStatementNode Visit(ProgramStatement programStatement)
	{
		var moduleScopeCount = 0;
		ModuleSymbol? moduleSymbol = null;
		if (programStatement.moduleStatement is { } moduleStatement)
		{
			var tryLookup = true;
			foreach (var moduleIdentifier in moduleStatement.scope.identifiers)
			{
				Scope scope;
				if (tryLookup && CurrentScope.LookupSymbol(moduleIdentifier.Text, out moduleSymbol, out _))
				{
					if (moduleSymbol is null)
					{
						// The symbol exists but is not a module: error!
						throw NewCollectionException($"Cannot define module named '{moduleStatement.scope}'; " +
						                              "A symbol with that name is already declared in this scope");
					}

					scope = moduleSymbol.Scope;
				}
				else
				{
					tryLookup = false;
					scope = new Scope(CurrentScope);
					moduleSymbol = new ModuleSymbol(moduleIdentifier.Text, scope);
					CurrentScope.AddSymbol(moduleSymbol);
				}

				scopeStack.Push(scope);
				moduleScopeCount++;
			}
		}

		var topLevelStatements = new List<CollectedStatementNode>();
		foreach (var topLevelStatement in programStatement.topLevelStatements)
		{
			try
			{
				topLevelStatements.Add(topLevelStatement.Accept(this));
			}
			catch (CollectionException e)
			{
				exceptions.Add(e);

				while (scopeStack.Count > moduleScopeCount + 1)
				{
					scopeStack.Pop();
				}
			}
		}

		while (moduleScopeCount-- > 0)
			scopeStack.Pop();

		return new CollectedProgramStatement(programStatement.importStatements, moduleSymbol, topLevelStatements);
	}

	public CollectedStatementNode Visit(ImportStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(AggregateImportStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(DllImportStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(ExternalFunctionStatement statement)
	{
		var externalFunctionSymbol = new ExternalFunctionSymbol(statement.identifier.Text, statement.attributes,
			statement.identifier.Source, statement.signatureRange);

		statement.identifier.Symbol = externalFunctionSymbol;

		if (CurrentScope.LookupSymbol(externalFunctionSymbol.Name) is ExternalFunctionSymbol existingFunctionSymbol)
			existingFunctionSymbol.Overloads.Add(existingFunctionSymbol);
		else
			CurrentScope.AddSymbol(externalFunctionSymbol);
		
		return new CollectedExternalFunctionStatement(externalFunctionSymbol, statement);
	}

	public CollectedStatementNode Visit(ModuleStatement moduleStatement)
	{
		var moduleScopeCount = 0;
		ModuleSymbol? moduleSymbol = null;
		var tryLookup = true;
		foreach (var moduleIdentifier in moduleStatement.scope.identifiers)
		{
			Scope scope;
			if (tryLookup && CurrentScope.LookupSymbol(moduleIdentifier.Text, out moduleSymbol, out _))
			{
				if (moduleSymbol is null)
				{
					// The symbol exists but is not a module: error!
					throw NewCollectionException($"Cannot define module named '{moduleStatement.scope}'; " +
					                              "A symbol with that name is already declared in this scope");
				}

				scope = moduleSymbol.Scope;
			}
			else
			{
				tryLookup = false;
				scope = new Scope(CurrentScope);
				moduleSymbol = new ModuleSymbol(moduleIdentifier.Text, scope);
				CurrentScope.AddSymbol(moduleSymbol);
			}

			moduleIdentifier.Symbol = moduleSymbol;
			scopeStack.Push(scope);
			moduleScopeCount++;
		}
		
		var topLevelStatements = new List<CollectedStatementNode>();
		foreach (var topLevelStatement in moduleStatement.topLevelStatements)
		{
			topLevelStatements.Add(topLevelStatement.Accept(this));
		}
		
		while (moduleScopeCount-- > 0)
			scopeStack.Pop();

		return new CollectedModuleStatement(moduleSymbol!, topLevelStatements);
	}

	public CollectedStatementNode Visit(EntryStatement entryStatement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);
		
		var statements = new List<CollectedStatementNode>();
		foreach (var statement in entryStatement.body.statements)
		{
			statements.Add(statement.Accept(this));
		}

		scopeStack.Pop();
		
		return new CollectedEntryStatement(entryStatement, scope, statements);
	}

	public CollectedStatementNode Visit(FunctionDeclarationStatement functionDeclarationStatement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);

		var body = functionDeclarationStatement.body.Accept(this);

		scopeStack.Pop();
		
		var functionSymbol = new FunctionSymbol(functionDeclarationStatement.identifier.Text, scope,
			functionDeclarationStatement.identifier.Source, functionDeclarationStatement.signatureRange);

		functionDeclarationStatement.identifier.Symbol = functionSymbol;

		if (CurrentScope.LookupSymbol(functionSymbol.Name) is FunctionSymbol existingFunctionSymbol)
		{
			existingFunctionSymbol.Overloads.Add(existingFunctionSymbol);
		}
		else
		{
			try
			{
				CurrentScope.AddSymbol(functionSymbol);
			}
			catch (ArgumentException)
			{
				throw NewCollectionException($"A symbol named '{functionSymbol.Name} already exists in this scope",
					functionDeclarationStatement.identifier);
			}
		}

		return new CollectedFunctionDeclarationStatement(functionDeclarationStatement, functionSymbol, body);
	}

	public CollectedStatementNode Visit(ConstructorDeclarationStatement statement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);

		var body = statement.body.Accept(this);

		scopeStack.Pop();
		
		return new CollectedConstructorDeclarationStatement(statement, scope, body);
	}

	public CollectedStatementNode Visit(DestructorDeclarationStatement statement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);

		var body = statement.body.Accept(this);

		scopeStack.Pop();
		
		return new CollectedDestructorDeclarationStatement(statement, scope, body);
	}

	public CollectedStatementNode Visit(ExpressionStatement statement)
	{
		return new CollectedExpressionStatement(statement);
	}

	public CollectedStatementNode Visit(BlockStatement statement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);
		
		var statements = new List<CollectedStatementNode>();
		foreach (var bodyStatement in statement.statements)
		{
			statements.Add(bodyStatement.Accept(this));
		}

		scopeStack.Pop();
		
		return new CollectedBlockStatement(scope, statements);
	}

	public CollectedStatementNode Visit(IfStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(MutableVarDeclarationStatement statement)
	{
		var name = statement.identifier.Text;
		var symbol = new VarSymbol(name, true);
		statement.identifier.Symbol = symbol;
		
		// Todo: Shadowing
		if (CurrentScope.LookupSymbol(name) is not { } existingSymbol)
		{
			CurrentScope.AddSymbol(symbol);
		}
		else
		{
			throw NewCollectionException($"Cannot define a local variable named '{name}'; " +
			                             $"There is already {existingSymbol.SymbolTypeName} with the same name " +
			                             "defined in this scope", statement.identifier);
		}

		return new CollectedVarDeclarationStatement(symbol, statement.identifier, statement.type,
			statement.initializer);
	}

	public CollectedStatementNode Visit(ImmutableVarDeclarationStatement statement)
	{
		var name = statement.identifier.Text;
		var symbol = new VarSymbol(name, false);
		statement.identifier.Symbol = symbol;
		
		// Todo: Shadowing
		if (CurrentScope.LookupSymbol(name) is not { } existingSymbol)
		{
			CurrentScope.AddSymbol(symbol);
		}
		else
		{
			throw NewCollectionException($"Cannot define a local variable named '{name}'; " +
			                             $"There is already {existingSymbol.SymbolTypeName} with the same name " +
			                             "defined in this scope", statement.identifier);
		}

		return new CollectedVarDeclarationStatement(symbol, statement.identifier, statement.type,
			statement.initializer);
	}

	public CollectedStatementNode Visit(ConstVarDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(ReturnStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(StructDeclarationStatement structDeclarationStatement)
	{
		var scope = new Scope(CurrentScope);
		var name = structDeclarationStatement.identifier.Text;
		var structSymbol = new StructSymbol(name, structDeclarationStatement.isMutable, scope);
		structDeclarationStatement.identifier.Symbol = structSymbol;
		
		if (CurrentScope.LookupSymbol(name) is not { } existingSymbol)
		{
			CurrentScope.AddSymbol(structSymbol);
		}
		else
		{
			throw NewCollectionException($"Cannot define a struct named '{name}'; " +
			                             $"There is already {existingSymbol.SymbolTypeName} with the same name " +
			                             "defined in this scope", structDeclarationStatement.identifier);
		}
		
		scopeStack.Push(scope);
		typeStack.Push(structSymbol);

		var statements = new List<CollectedStatementNode>();
		foreach (var topLevelStatement in structDeclarationStatement.statements)
		{
			statements.Add(topLevelStatement.Accept(this));
		}
		
		typeStack.Pop();
		scopeStack.Pop();
		return new CollectedStructDeclarationStatement(structSymbol, statements);
	}

	public CollectedStatementNode Visit(InterfaceDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(CastDeclarationStatement statement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);

		var body = statement.body.Accept(this);

		scopeStack.Pop();
		
		return new CollectedCastDeclarationStatement(statement, scope, body);
	}

	public CollectedStatementNode Visit(StringDeclarationStatement statement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);

		var body = statement.body.Accept(this);

		scopeStack.Pop();
		
		return new CollectedStringDeclarationStatement(statement, scope, body);
	}

	public CollectedStatementNode Visit(OperatorDeclarationStatement statement)
	{
		var scope = new Scope(CurrentScope);
		scopeStack.Push(scope);

		var body = statement.body.Accept(this);

		scopeStack.Pop();
		
		return new CollectedOperatorDeclarationStatement(statement, scope, body);
	}

	public CollectedStatementNode Visit(FieldDeclarationStatement statement)
	{
		var name = statement.identifier.Text;
		var syntaxType = statement.type;
		if (CurrentScope.LookupSymbol(name, out FieldSymbol? _, out var existingSymbol))
		{
			throw NewCollectionException($"Cannot define a field named '{name}'; " +
			                              $"There is already {existingSymbol.SymbolTypeName} with the same name " +
			                              "defined in this scope", statement.identifier);
		}

		switch (statement.mutability)
		{
			case FieldDeclarationStatement.Mutability.Mutable:
				var mutableFieldSymbol = new FieldSymbol(name, true, statement.isStatic, CurrentType,
					statement.isStatic ? 0u : CurrentType.NextFieldIndex());
				
				statement.identifier.Symbol = mutableFieldSymbol;
				CurrentScope.AddSymbol(mutableFieldSymbol);
				return new CollectedFieldDeclarationStatement(mutableFieldSymbol, syntaxType, statement.initializer,
					statement.range);
			
			case FieldDeclarationStatement.Mutability.Immutable:
				var immutableFieldSymbol = new FieldSymbol(name, false, statement.isStatic, CurrentType,
					statement.isStatic ? 0u : CurrentType.NextFieldIndex());
				
				statement.identifier.Symbol = immutableFieldSymbol;
				CurrentScope.AddSymbol(immutableFieldSymbol);
				return new CollectedFieldDeclarationStatement(immutableFieldSymbol, syntaxType, statement.initializer,
					statement.range);
			
			case FieldDeclarationStatement.Mutability.Constant:
				if (statement.initializer is null)
					throw NewCollectionException("Constant fields require an initializer", statement.identifier);
				
				var constantSymbol = new ConstSymbol(name);
				statement.identifier.Symbol = constantSymbol;
				CurrentScope.AddSymbol(constantSymbol);
				return new CollectedConstDeclarationStatement(constantSymbol, syntaxType, statement.initializer,
					statement.range);
			
			default:
				throw NewCollectionException("Invalid field mutability; This should never happen! " +
				                              "Please report this to the compiler developer");
		}
	}

	public CollectedStatementNode Visit(DefineStatement statement)
	{
		var symbol = new DefSymbol(statement.identifier.Text);
		statement.identifier.Symbol = symbol;
		CurrentScope.AddSymbol(symbol);
		return new CollectedDefStatement(symbol, statement.type);
	}
}