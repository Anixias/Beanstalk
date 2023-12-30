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

	private static string FormatMessage(string message, Token? token)
	{
		if (token is null)
			return message;

		return $"[line {token.Line}, column {token.Column} at '{token.Text}'] {message}";
	}
}

/// <summary>
/// Collects symbols and scopes from a list of <see cref="Beanstalk.Analysis.Syntax.Ast"/> instances
/// </summary>
public partial class Collector : StatementNode.IVisitor<CollectedStatementNode>
{
	public readonly bool is64Bit;
	public readonly Scope globalScope = new();
	private readonly Stack<Scope> scopeStack = new();
	private Scope CurrentScope => scopeStack.Peek();
	public readonly List<CollectionException> exceptions = [];
	private string currentWorkingDirectory = "";
	private string currentFilePath = "";

	public Collector(bool is64Bit)
	{
		scopeStack.Push(globalScope);
		this.is64Bit = is64Bit;
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

		var nint = new AliasedSymbol(TokenType.KeywordNInt.ToString(), is64Bit ? TypeSymbol.Int64 : TypeSymbol.Int32);
		var nuint = new AliasedSymbol(TokenType.KeywordNUInt.ToString(),
			is64Bit ? TypeSymbol.UInt64 : TypeSymbol.UInt32);
		scope.SymbolTable.Add(nint);
		scope.SymbolTable.Add(nuint);
	}

	private CollectionException NewCollectionException(string message, Token? token = null)
	{
		return new CollectionException(message, token, currentWorkingDirectory, currentFilePath);
	}

	public CollectedAst? Collect(Ast ast, string workingDirectory, string filePath)
	{
		try
		{
			currentWorkingDirectory = workingDirectory;
			currentFilePath = filePath;

			var root = ast.Root switch
			{
				StatementNode statementNode => statementNode.Accept(this),
				_ => null
			};

			if (scopeStack.Count != 1)
				throw NewCollectionException("Invalid operation: Scope stack unbalanced");

			if (root is null)
				return null;

			return new CollectedAst(root, workingDirectory, filePath);
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
			topLevelStatements.Add(topLevelStatement.Accept(this));
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

	public CollectedStatementNode Visit(DllImportStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(ExternalFunctionStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
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

	public CollectedStatementNode Visit(EntryStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(FunctionDeclarationStatement statement)
	{
		return new CollectedFunctionDeclarationStatement(statement);
	}

	public CollectedStatementNode Visit(ConstructorDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(DestructorDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(ExpressionStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(BlockStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(IfStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(MutableVarDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(ImmutableVarDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
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
		var structSymbol = new StructSymbol(structDeclarationStatement.identifier.Text,
			structDeclarationStatement.isMutable, scope);
		
		CurrentScope.AddSymbol(structSymbol);
		
		scopeStack.Push(scope);

		var statements = new List<CollectedStatementNode>();
		foreach (var topLevelStatement in structDeclarationStatement.statements)
		{
			statements.Add(topLevelStatement.Accept(this));
		}
		
		scopeStack.Pop();
		return new CollectedStructStatement(structSymbol, statements);
	}

	public CollectedStatementNode Visit(InterfaceDeclarationStatement statement)
	{
		// Todo
		return new CollectedSimpleStatement(statement);
	}

	public CollectedStatementNode Visit(CastDeclarationStatement statement)
	{
		return new CollectedCastDeclarationStatement(statement);
	}

	public CollectedStatementNode Visit(OperatorDeclarationStatement statement)
	{
		return new CollectedOperatorDeclarationStatement(statement);
	}

	public CollectedStatementNode Visit(FieldDeclarationStatement statement)
	{
		var name = statement.identifier.Text;
		var syntaxType = statement.type;
		if (CurrentScope.LookupSymbol(name, out FieldSymbol? _, out var existingSymbol))
		{
			throw NewCollectionException($"Cannot define a field named '{name}'; " +
			                              $"There is already {existingSymbol.SymbolTypeName} with the same name " +
			                              "defined in this scope");
		}

		switch (statement.mutability)
		{
			case FieldDeclarationStatement.Mutability.Mutable:
				var mutableFieldSymbol = new FieldSymbol(name, true, statement.isStatic);
				CurrentScope.AddSymbol(mutableFieldSymbol);
				return new CollectedFieldDeclarationStatement(mutableFieldSymbol, syntaxType);
			case FieldDeclarationStatement.Mutability.Immutable:
				var immutableFieldSymbol = new FieldSymbol(name, false, statement.isStatic);
				CurrentScope.AddSymbol(immutableFieldSymbol);
				return new CollectedFieldDeclarationStatement(immutableFieldSymbol, syntaxType);
			case FieldDeclarationStatement.Mutability.Constant:
				var constantSymbol = new ConstSymbol(name, statement.isStatic);
				CurrentScope.AddSymbol(constantSymbol);
				return new CollectedConstDeclarationStatement(constantSymbol, syntaxType);
			default:
				throw NewCollectionException("Invalid field mutability; This should never happen! " +
				                              "Please report this to the compiler developer");
		}
	}

	public CollectedStatementNode Visit(DefineStatement statement)
	{
		var symbol = new DefSymbol(statement.identifier.Text);
		CurrentScope.AddSymbol(symbol);
		return new CollectedDefStatement(symbol, statement.type);
	}
}