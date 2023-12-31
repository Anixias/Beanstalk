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

	public void Visit(CollectedFunctionDeclarationStatement statement)
	{
		var functionDeclarationStatement = statement.functionDeclarationStatement;
		var body = new Scope(CurrentScope);
		scopeStack.Push(body);

		var typeParameterSymbols = new List<TypeParameterSymbol>();
		foreach (var typeParameter in functionDeclarationStatement.typeParameters)
		{
			var typeParameterSymbol = new TypeParameterSymbol(typeParameter.Text);
			typeParameterSymbols.Add(typeParameterSymbol);
			body.AddSymbol(typeParameterSymbol);
		}

		var parameters = new List<ParameterSymbol>();
		var requireDefault = false;
		var additionalParametersAllowed = true;
		foreach (var parameter in functionDeclarationStatement.parameters)
		{
			if (!additionalParametersAllowed)
				exceptions.Add(NewCollectionException("Cannot define additional parameters after a " +
				                                      "variadic parameter", parameter.identifier));
			
			var varSymbol = new VarSymbol(parameter.identifier.Text, parameter.isMutable);

			if (parameter.type is not null)
			{
				var invalidTypes = new List<Token>();
				varSymbol.Type = ResolveType(parameter.type, invalidTypes);

				if (invalidTypes.Count > 0)
				{
					foreach (var invalidType in invalidTypes)
					{
						exceptions.Add(NewCollectionException(
							$"Could not find a type named '{invalidType.Text}'", invalidType));
					}
				}

				if (parameter.isVariadic)
				{
					additionalParametersAllowed = false;
					
					if (varSymbol.Type is not null && varSymbol.Type is not ArrayType)
						exceptions.Add(NewCollectionException("Variadic parameters must be array types",
							parameter.identifier));
				}
			}

			if (parameter.defaultExpression is { } defaultExpression)
			{
				requireDefault = true;
				parameters.Add(new ParameterSymbol(varSymbol, defaultExpression));
				continue;
			}
			
			if (requireDefault)
				exceptions.Add(NewCollectionException(
					$"Parameter '{parameter.identifier.Text}' requires a default value because a " +
					"previous parameter specified a default value", parameter.identifier));

			parameters.Add(new ParameterSymbol(varSymbol, null));
		}

		Type? returnType = null;
		if (functionDeclarationStatement.returnType is not null)
		{
			var invalidTypes = new List<Token>();
			returnType = ResolveType(functionDeclarationStatement.returnType, invalidTypes);
			
			if (invalidTypes.Count > 0)
			{
				foreach (var invalidType in invalidTypes)
				{
					exceptions.Add(NewCollectionException(
						$"Could not find a type named '{invalidType.Text}'", invalidType));
				}
			}
		}

		foreach (var parameter in parameters)
		{
			body.AddSymbol(parameter);
		}
		
		var functionSymbol = new FunctionSymbol(functionDeclarationStatement.identifier.Text,
			typeParameterSymbols, parameters, body)
		{
			ReturnType = returnType
		};

		statement.functionSymbol = functionSymbol;
		scopeStack.Pop();

		if (CurrentScope.LookupSymbol(functionSymbol.Name) is FunctionSymbol existingFunctionSymbol)
		{
			if (functionSymbol.SignatureMatches(existingFunctionSymbol))
				throw NewCollectionException("Function signature matches existing function signature",
					functionDeclarationStatement.identifier);

			foreach (var existingOverload in existingFunctionSymbol.Overloads)
			{
				if (functionSymbol.SignatureMatches(existingOverload))
					throw NewCollectionException("Function signature matches existing function signature",
						functionDeclarationStatement.identifier);
			}
			
			existingFunctionSymbol.Overloads.Add(functionSymbol);
		}
		else
		{
			CurrentScope.AddSymbol(functionSymbol);
		}
	}

	public void Visit(CollectedConstructorDeclarationStatement statement)
	{
		var constructorDeclarationStatement = statement.constructorDeclarationStatement;
		var body = new Scope(CurrentScope);
		scopeStack.Push(body);

		var parameters = new List<ParameterSymbol>();
		var requireDefault = false;
		var additionalParametersAllowed = true;
		foreach (var parameter in constructorDeclarationStatement.parameters)
		{
			if (!additionalParametersAllowed)
				exceptions.Add(NewCollectionException("Cannot define additional parameters after a " +
				                                      "variadic parameter", parameter.identifier));
			
			var varSymbol = new VarSymbol(parameter.identifier.Text, parameter.isMutable);

			if (parameter.type is not null)
			{
				var invalidTypes = new List<Token>();
				varSymbol.Type = ResolveType(parameter.type, invalidTypes);

				if (invalidTypes.Count > 0)
				{
					foreach (var invalidType in invalidTypes)
					{
						exceptions.Add(NewCollectionException(
							$"Could not find a type named '{invalidType.Text}'", invalidType));
					}
				}

				if (parameter.isVariadic)
				{
					additionalParametersAllowed = false;
					
					if (varSymbol.Type is not null && varSymbol.Type is not ArrayType)
						exceptions.Add(NewCollectionException("Variadic parameters must be array types",
							parameter.identifier));
				}
			}

			if (parameter.defaultExpression is { } defaultExpression)
			{
				requireDefault = true;
				parameters.Add(new ParameterSymbol(varSymbol, defaultExpression));
				continue;
			}
			
			if (requireDefault)
				exceptions.Add(NewCollectionException(
					$"Parameter '{parameter.identifier.Text}' requires a default value because a " +
					"previous parameter specified a default value", parameter.identifier));

			parameters.Add(new ParameterSymbol(varSymbol, null));
		}

		foreach (var parameter in parameters)
		{
			body.AddSymbol(parameter);
		}

		var constructorSymbol = new ConstructorSymbol(parameters, body);

		statement.constructorSymbol = constructorSymbol;
		scopeStack.Pop();

		if (CurrentScope.LookupSymbol(constructorSymbol.Name) is ConstructorSymbol existingConstructorSymbol)
		{
			if (constructorSymbol.SignatureMatches(existingConstructorSymbol))
				throw NewCollectionException("Constructor signature matches existing constructor signature",
					constructorDeclarationStatement.constructorKeyword);

			foreach (var existingOverload in existingConstructorSymbol.Overloads)
			{
				if (constructorSymbol.SignatureMatches(existingOverload))
					throw NewCollectionException("Constructor signature matches existing constructor signature",
						constructorDeclarationStatement.constructorKeyword);
			}
			
			existingConstructorSymbol.Overloads.Add(constructorSymbol);
		}
		else
		{
			CurrentScope.AddSymbol(constructorSymbol);
		}
	}

	public void Visit(CollectedDestructorDeclarationStatement statement)
	{
		var destructorDeclarationStatement = statement.destructorDeclarationStatement;
		var body = new Scope(CurrentScope);
		scopeStack.Push(body);

		var destructorSymbol = new DestructorSymbol(body);

		statement.destructorSymbol = destructorSymbol;
		scopeStack.Pop();

		if (CurrentScope.LookupSymbol(destructorSymbol.Name) is DestructorSymbol)
		{
			exceptions.Add(NewCollectionException("A destructor is already declared in this scope",
				destructorDeclarationStatement.destructorKeyword));

			return;
		}
		
		CurrentScope.AddSymbol(destructorSymbol);
	}

	public void Visit(CollectedCastDeclarationStatement statement)
	{
		var castDeclarationStatement = statement.castDeclarationStatement;
		var body = new Scope(CurrentScope);

		var parameter = castDeclarationStatement.parameter;
				
		if (parameter.isMutable)
			exceptions.Add(NewCollectionException(
				"Cannot mark cast parameter as mutable", parameter.identifier));
				
		if (parameter.isVariadic)
			exceptions.Add(NewCollectionException(
				"Cannot mark cast parameter as variadic", parameter.identifier));
				
		var varSymbol = new VarSymbol(parameter.identifier.Text, false);
		if (parameter.type is not null)
		{
			var invalidTypes = new List<Token>();
			varSymbol.Type = ResolveType(parameter.type, invalidTypes);

			if (invalidTypes.Count > 0)
			{
				foreach (var invalidType in invalidTypes)
				{
					exceptions.Add(NewCollectionException(
						$"Could not find a type named '{invalidType.Text}'", invalidType));
				}
			}
		}
		else
		{
			exceptions.Add(NewCollectionException(
				"Parameter type is required in cast overload declarations", parameter.identifier));
		}

		var invalidReturnTypes = new List<Token>();
		var returnType = ResolveType(castDeclarationStatement.returnSyntaxType, invalidReturnTypes);
				
		if (invalidReturnTypes.Count > 0)
		{
			foreach (var invalidType in invalidReturnTypes)
			{
				exceptions.Add(NewCollectionException(
					$"Could not find a type named '{invalidType.Text}'", invalidType));
			}
		}

		if (returnType is null)
			return;
				
		var parameterSymbol = new ParameterSymbol(varSymbol, null);
		body.AddSymbol(parameterSymbol);

		var castSymbol = new CastOverloadSymbol(castDeclarationStatement.isImplicit, parameterSymbol,
			returnType, body);

		CurrentScope.AddSymbol(castSymbol);
		statement.castOverloadSymbol = castSymbol;
	}

	public void Visit(CollectedOperatorDeclarationStatement statement)
	{
		var operatorDeclarationStatement = statement.operatorDeclarationStatement;
		var body = new Scope(CurrentScope);
		switch (operatorDeclarationStatement.operation)
		{
			case BinaryOperationExpression binaryOperationExpression:
			{
				var left = binaryOperationExpression.left;
				var right = binaryOperationExpression.right;
				
				if (left.isVariadic)
					exceptions.Add(NewCollectionException(
						"Cannot mark operator parameter as variadic", left.identifier));
				
				if (right.isVariadic)
					exceptions.Add(NewCollectionException(
						"Cannot mark operator parameter as variadic", left.identifier));
				
				var leftSymbol = new VarSymbol(left.identifier.Text, false);
				if (left.type is not null)
				{
					var invalidTypes = new List<Token>();
					leftSymbol.Type = ResolveType(left.type, invalidTypes);

					if (invalidTypes.Count > 0)
					{
						foreach (var invalidType in invalidTypes)
						{
							exceptions.Add(NewCollectionException(
								$"Could not find a type named '{invalidType.Text}'", invalidType));
						}
					}
				}
				else
				{
					exceptions.Add(NewCollectionException(
						"Parameter type is required in operator overload declarations", left.identifier));
				}
				
				var rightSymbol = new VarSymbol(right.identifier.Text, false);
				if (right.type is not null)
				{
					var invalidTypes = new List<Token>();
					rightSymbol.Type = ResolveType(right.type, invalidTypes);

					if (invalidTypes.Count > 0)
					{
						foreach (var invalidType in invalidTypes)
						{
							exceptions.Add(NewCollectionException(
								$"Could not find a type named '{invalidType.Text}'", invalidType));
						}
					}
				}
				else
				{
					exceptions.Add(NewCollectionException(
						"Parameter type is required in operator overload declarations", right.identifier));
				}
				
				var invalidReturnTypes = new List<Token>();
				var returnType = ResolveType(operatorDeclarationStatement.returnSyntaxType, invalidReturnTypes);
				
				if (invalidReturnTypes.Count > 0)
				{
					foreach (var invalidType in invalidReturnTypes)
					{
						exceptions.Add(NewCollectionException(
							$"Could not find a type named '{invalidType.Text}'", invalidType));
					}
				}

				if (returnType is null)
					return;
						
				var leftParameterSymbol = new ParameterSymbol(leftSymbol, null);
				var rightParameterSymbol = new ParameterSymbol(rightSymbol, null);
				body.AddSymbol(leftParameterSymbol);
				body.AddSymbol(rightParameterSymbol);

				var operatorSymbol = new BinaryOperatorOverloadSymbol(leftParameterSymbol,
					binaryOperationExpression.operation, rightParameterSymbol, returnType, body);

				CurrentScope.AddSymbol(operatorSymbol);
				statement.operatorOverloadSymbol = operatorSymbol;
			}
				break;

			case UnaryOperationExpression unaryOperationExpression:
			{
				var operand = unaryOperationExpression.operand;
				
				if (operand.isVariadic)
					exceptions.Add(NewCollectionException(
						"Cannot mark operator parameter as variadic", operand.identifier));
				
				var varSymbol = new VarSymbol(operand.identifier.Text, false);
				if (operand.type is not null)
				{
					var invalidTypes = new List<Token>();
					varSymbol.Type = ResolveType(operand.type, invalidTypes);

					if (invalidTypes.Count > 0)
					{
						foreach (var invalidType in invalidTypes)
						{
							exceptions.Add(NewCollectionException(
								$"Could not find a type named '{invalidType.Text}'", invalidType));
						}
					}
				}
				else
				{
					exceptions.Add(NewCollectionException(
						"Parameter type is required in operator overload declarations", operand.identifier));
				}
				
				var invalidReturnTypes = new List<Token>();
				var returnType = ResolveType(operatorDeclarationStatement.returnSyntaxType, invalidReturnTypes);
				
				if (invalidReturnTypes.Count > 0)
				{
					foreach (var invalidType in invalidReturnTypes)
					{
						exceptions.Add(NewCollectionException(
							$"Could not find a type named '{invalidType.Text}'", invalidType));
					}
				}

				if (returnType is null)
					return;
				
				var parameterSymbol = new ParameterSymbol(varSymbol, null);
				body.AddSymbol(parameterSymbol);

				var operatorSymbol = new UnaryOperatorOverloadSymbol(parameterSymbol,
					unaryOperationExpression.operation, unaryOperationExpression.isPrefix, returnType, body);

				CurrentScope.AddSymbol(operatorSymbol);
				statement.operatorOverloadSymbol = operatorSymbol;
			}
				break;
			
			default:
				exceptions.Add(NewCollectionException(
					"Invalid operation: Unknown operation expression type",
					statement.operatorDeclarationStatement.operation.op));
				
				break;
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