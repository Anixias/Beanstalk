using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public partial class Collector : CollectedStatementNode.IVisitor
{
	private readonly SymbolTable importedSymbols = new();
	
	public void Collect(CollectedAst ast)
	{
		try
		{
			importedSymbols.Clear();
			currentWorkingDirectory = ast.WorkingDirectory;
			currentFilePath = ast.FilePath;
			
			switch (ast.Root)
			{
				case CollectedStatementNode statementNode:
					statementNode.Accept(this);
					break;
			}

			if (scopeStack.Count != 1)
				throw NewCollectionException("Invalid operation: Scope stack unbalanced");
		}
		catch (CollectionException e)
		{
			while (scopeStack.Count > 1)
				scopeStack.Pop();

			exceptions.Add(e);
		}
		finally
		{
			currentWorkingDirectory = "";
			currentFilePath = "";
			importedSymbols.Clear();
		}
	}

	private void HandleImport(ImportStatement statement)
	{
		var scopeCount = 0;
		foreach (var module in statement.scope.identifiers)
		{
			if (!CurrentScope.LookupSymbol(module.Text, out ModuleSymbol? moduleSymbol, out _) || moduleSymbol is null)
				throw NewCollectionException($"Could not find a module named {module.Text}");

			scopeStack.Push(moduleSymbol.Scope);
			scopeCount++;
		}

		if (statement.identifier.Type == TokenType.OpStar)
		{
			// Import all functions and types from the module
			foreach (var symbol in CurrentScope.SymbolTable.Values)
			{
				importedSymbols.Add(symbol);
			}
		}
		else
		{
			if (CurrentScope.LookupSymbol(statement.identifier.Text) is not { } importedSymbol)
				throw NewCollectionException(
					$"Could not find a symbol named {statement.identifier.Text} in module {statement.scope.text}");

			if (statement.alias is { } alias)
			{
				var aliasSymbol = new AliasedSymbol(alias.Text, importedSymbol);
				importedSymbols.Add(aliasSymbol);
			}
			else
			{
				importedSymbols.Add(importedSymbol);
			}
		}

		while (scopeCount-- > 0)
			scopeStack.Pop();
	}

	private ISymbol? LookupSymbolWithImports(string name)
	{
		if (CurrentScope.LookupSymbol(name) is { } symbol)
		{
			if (symbol is AliasedSymbol alias)
				return alias.LinkedSymbol;
			
			return symbol;
		}

		var importedSymbol = importedSymbols.Lookup(name);
		if (importedSymbol is AliasedSymbol aliasedImport)
			return aliasedImport.LinkedSymbol;

		return importedSymbol;
	}
	
	public void Visit(CollectedProgramStatement statement)
	{
		foreach (var importStatement in statement.importStatements)
		{
			HandleImport(importStatement);
		}

		var inModule = false;
		if (statement.moduleSymbol is not null)
		{
			scopeStack.Push(statement.moduleSymbol.Scope);
			inModule = true;
		}

		foreach (var topLevelStatement in statement.topLevelStatements)
		{
			topLevelStatement.Accept(this);
		}

		if (inModule)
			scopeStack.Pop();
	}

	public void Visit(CollectedModuleStatement statement)
	{
		scopeStack.Push(statement.moduleSymbol.Scope);

		foreach (var topLevelStatement in statement.topLevelStatements)
		{
			topLevelStatement.Accept(this);
		}

		scopeStack.Pop();
	}

	public void Visit(CollectedStructStatement structStatement)
	{
		scopeStack.Push(structStatement.structSymbol.Scope);

		foreach (var statement in structStatement.statements)
		{
			statement.Accept(this);
		}

		scopeStack.Pop();
	}

	public void Visit(CollectedFieldDeclarationStatement statement)
	{
		if (statement.syntaxType is not { } syntaxType)
			return;

		var invalidTypes = new List<Token>();
		statement.fieldSymbol.Type = ResolveType(syntaxType, invalidTypes);

		if (invalidTypes.Count == 0)
			return;

		foreach (var invalidType in invalidTypes)
		{
			exceptions.Add(NewCollectionException($"Could not find a type named '{invalidType.Text}'", invalidType));
		}
	}

	public void Visit(CollectedConstDeclarationStatement statement)
	{
		if (statement.syntaxType is not { } syntaxType)
			return;

		var invalidTypes = new List<Token>();
		statement.constSymbol.Type = ResolveType(syntaxType, invalidTypes);

		if (invalidTypes.Count == 0)
			return;

		foreach (var invalidType in invalidTypes)
		{
			exceptions.Add(NewCollectionException($"Could not find a type named '{invalidType.Text}'", invalidType));
		}
	}

	public void Visit(CollectedDefStatement statement)
	{
		var invalidTypes = new List<Token>();
		statement.defSymbol.Type = ResolveType(statement.syntaxType, invalidTypes);

		if (invalidTypes.Count == 0)
			return;

		foreach (var invalidType in invalidTypes)
		{
			exceptions.Add(NewCollectionException($"Could not find a type named '{invalidType.Text}'", invalidType));
		}
	}

	public void Visit(CollectedSimpleStatement statement)
	{
		// Do nothing
	}
	
	private Type? ResolveType(SyntaxType syntaxType, ICollection<Token> invalidTypes)
	{
		switch (syntaxType)
		{
			case ArraySyntaxType arraySyntaxType:
				if (ResolveType(arraySyntaxType.baseSyntaxType, invalidTypes) is not { } arrayBaseType)
					return null;
				
				if (invalidTypes.Count > 0)
					return null;

				return new ArrayType(arrayBaseType);

			case NullableSyntaxType nullableSyntaxType:
				if (ResolveType(nullableSyntaxType.baseSyntaxType, invalidTypes) is not { } nullableBaseType)
					return null;

				if (invalidTypes.Count > 0)
					return null;
				
				return new NullableType(nullableBaseType);

			case MutableSyntaxType mutableSyntaxType:
				if (ResolveType(mutableSyntaxType.baseSyntaxType, invalidTypes) is not { } mutableBaseType)
					return null;

				if (invalidTypes.Count > 0)
					return null;
				
				return new MutableType(mutableBaseType);

			case ReferenceSyntaxType referenceSyntaxType:
				if (ResolveType(referenceSyntaxType.baseSyntaxType, invalidTypes) is not { } referenceBaseType)
					return null;

				if (invalidTypes.Count > 0)
					return null;
				
				return new ReferenceType(referenceBaseType, referenceSyntaxType.immutable);

			case GenericSyntaxType genericSyntaxType:
				if (ResolveType(genericSyntaxType.baseSyntaxType, invalidTypes) is not { } genericBaseType)
					return null;

				var typeParameters = new List<Type>();
				foreach (var typeParameterSyntax in genericSyntaxType.typeParameters)
				{
					if (ResolveType(typeParameterSyntax, invalidTypes) is not { } typeParameter)
						continue;
					
					typeParameters.Add(typeParameter);
				}
				
				if (invalidTypes.Count > 0)
					return null;
				
				return new GenericType(genericBaseType, typeParameters);

			case LambdaSyntaxType lambdaSyntaxType:
				var lambdaParameterTypes = new List<Type>();
				foreach (var lambdaParameterSyntax in lambdaSyntaxType.parameterTypes)
				{
					if (ResolveType(lambdaParameterSyntax, invalidTypes) is not { } lambdaParameterType)
						continue;
					
					lambdaParameterTypes.Add(lambdaParameterType);
				}

				Type? returnType = null;
				if (lambdaSyntaxType.returnType is { } returnSyntaxType)
				{
					if (ResolveType(returnSyntaxType, invalidTypes) is { } lambdaReturnType)
						returnType = lambdaReturnType;
				}
				
				if (invalidTypes.Count > 0)
					return null;
				
				return new LambdaType(lambdaParameterTypes, returnType);

			case TupleSyntaxType tupleSyntaxType:
				var types = new List<Type>();
				foreach (var type in tupleSyntaxType.types)
				{
					if (ResolveType(type, invalidTypes) is not { } tupleType)
						continue;

					types.Add(tupleType);
				}

				if (invalidTypes.Count > 0)
					return null;

				return new TupleType(types);
			
			case BaseSyntaxType baseSyntaxType:
				if (LookupSymbolWithImports(baseSyntaxType.token.Text) is not { } symbol)
				{
					invalidTypes.Add(baseSyntaxType.token);
					return null;
				}

				if (symbol is not TypeSymbol typeSymbol)
				{
					invalidTypes.Add(baseSyntaxType.token);
					return null;
				}

				return new BaseType(typeSymbol);
			
			default:
				return null;
		}
	}
}