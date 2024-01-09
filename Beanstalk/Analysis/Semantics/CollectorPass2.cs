using Beanstalk.Analysis.Syntax;
using Beanstalk.Analysis.Text;

namespace Beanstalk.Analysis.Semantics;

public partial class Collector : CollectedStatementNode.IVisitor
{
	public readonly SymbolTable importedSymbols = new();
	
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
			
			SymbolTable symbolTableToUpdate;
			if (statement.alias is { } groupingAlias)
			{
				var importGroupingSymbol = new ImportGroupingSymbol(groupingAlias.Text);
				symbolTableToUpdate = importGroupingSymbol.Symbols;
				importedSymbols.Add(importGroupingSymbol);
			}
			else
			{
				symbolTableToUpdate = importedSymbols;
			}
			
			foreach (var symbol in CurrentScope.SymbolTable.Values)
			{
				symbolTableToUpdate.Add(symbol);
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

	private void HandleAggregateImport(AggregateImportStatement statement)
	{
		var scopeCount = 0;
		foreach (var module in statement.scope.identifiers)
		{
			if (!CurrentScope.LookupSymbol(module.Text, out ModuleSymbol? moduleSymbol, out _) || moduleSymbol is null)
				throw NewCollectionException($"Could not find a module named {module.Text}");

			scopeStack.Push(moduleSymbol.Scope);
			scopeCount++;
		}

		SymbolTable symbolTableToUpdate;
		if (statement.alias is { } groupingAlias)
		{
			var importGroupingSymbol = new ImportGroupingSymbol(groupingAlias.Text);
			symbolTableToUpdate = importGroupingSymbol.Symbols;
			importedSymbols.Add(importGroupingSymbol);
		}
		else
		{
			symbolTableToUpdate = importedSymbols;
		}

		foreach (var importToken in statement.tokens)
		{
			if (CurrentScope.LookupSymbol(importToken.token.Text) is not { } importedSymbol)
				throw NewCollectionException(
					$"Could not find a symbol named {importToken.token.Text} in module {statement.scope.text}");

			if (importToken.alias is { } alias)
			{
				var aliasSymbol = new AliasedSymbol(alias.Text, importedSymbol);
				symbolTableToUpdate.Add(aliasSymbol);
			}
			else
			{
				symbolTableToUpdate.Add(importedSymbol);
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
			switch (importStatement)
			{
				case ImportStatement import:
					HandleImport(import);
					break;
				
				case AggregateImportStatement import:
					HandleAggregateImport(import);
					break;
				
				default:
					throw NewCollectionException("Invalid import statement", importStatement.range);
			}
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

		statement.importedSymbols = importedSymbols.Duplicate();
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

	public void Visit(CollectedStructDeclarationStatement structDeclarationStatement)
	{
		scopeStack.Push(structDeclarationStatement.structSymbol.Scope);
		typeStack.Push(structDeclarationStatement.structSymbol);

		foreach (var statement in structDeclarationStatement.statements)
		{
			statement.Accept(this);
		}

		typeStack.Pop();
		scopeStack.Pop();
	}

	public void Visit(CollectedFieldDeclarationStatement statement)
	{
		if (statement.syntaxType is not { } syntaxType)
			return;

		var invalidTypes = new List<Token>();
		statement.fieldSymbol.EvaluatedType = ResolveType(syntaxType, invalidTypes);

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
		statement.constSymbol.EvaluatedType = ResolveType(syntaxType, invalidTypes);

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
		statement.defSymbol.EvaluatedType = ResolveType(statement.syntaxType, invalidTypes);

		if (invalidTypes.Count == 0)
			return;

		foreach (var invalidType in invalidTypes)
		{
			exceptions.Add(NewCollectionException($"Could not find a type named '{invalidType.Text}'", invalidType));
		}
	}

	public void Visit(CollectedEntryStatement statement)
	{
		var entryStatement = statement.entryStatement;
		scopeStack.Push(statement.scope);

		var parameters = new List<ParameterSymbol>();
		foreach (var parameter in entryStatement.parameters)
		{
			if (parameter.isVariadic)
				exceptions.Add(NewCollectionException("Entry point cannot have variadic parameters",
					parameter.identifier));

			if (parameter.isMutable)
				exceptions.Add(NewCollectionException("Entry point cannot have mutable parameters",
					parameter.identifier));
			
			var varSymbol = new VarSymbol(parameter.identifier.Text, false);
			if (parameter.type is not null)
			{
				var invalidTypes = new List<Token>();
				varSymbol.EvaluatedType = ResolveType(parameter.type, invalidTypes);

				if (invalidTypes.Count > 0)
				{
					foreach (var invalidType in invalidTypes)
					{
						exceptions.Add(NewCollectionException(
							$"Could not find a type named '{invalidType.Text}'", invalidType));
					}
				}
			}

			if (parameter.defaultExpression is { } defaultExpression)
			{
				exceptions.Add(NewCollectionException("Entry point parameters cannot have a default value",
					parameter.identifier));
				
				parameters.Add(new ParameterSymbol(varSymbol, defaultExpression, false, (uint)parameters.Count));
				continue;
			}

			parameters.Add(new ParameterSymbol(varSymbol, null, false, (uint)parameters.Count));
		}

		foreach (var parameter in parameters)
		{
			statement.scope.AddSymbol(parameter);
		}

		var entrySymbol = new EntrySymbol(parameters, statement.scope);

		statement.entrySymbol = entrySymbol;
		scopeStack.Pop();
		CurrentScope.AddSymbol(entrySymbol);
	}

	public void Visit(CollectedFunctionDeclarationStatement statement)
	{
		var functionDeclarationStatement = statement.functionDeclarationStatement;
		scopeStack.Push(statement.scope);

		var typeParameterSymbols = new List<TypeParameterSymbol>();
		foreach (var typeParameter in functionDeclarationStatement.typeParameters)
		{
			var typeParameterSymbol = new TypeParameterSymbol(typeParameter.Text);
			typeParameterSymbols.Add(typeParameterSymbol);
			statement.scope.AddSymbol(typeParameterSymbol);
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
				varSymbol.EvaluatedType = ResolveType(parameter.type, invalidTypes);

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
					
					if (varSymbol.EvaluatedType is not null && varSymbol.EvaluatedType is not ArrayType)
						exceptions.Add(NewCollectionException("Variadic parameters must be array types",
							parameter.identifier));
				}
			}

			if (parameter.defaultExpression is { } defaultExpression)
			{
				requireDefault = true;
				parameters.Add(new ParameterSymbol(varSymbol, defaultExpression, parameter.isVariadic,
					(uint)parameters.Count));
				
				continue;
			}
			
			if (requireDefault)
				exceptions.Add(NewCollectionException(
					$"Parameter '{parameter.identifier.Text}' requires a default value because a " +
					"previous parameter specified a default value", parameter.identifier));

			parameters.Add(new ParameterSymbol(varSymbol, null, parameter.isVariadic, (uint)parameters.Count));
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
			statement.scope.AddSymbol(parameter);
		}
		
		var functionSymbol = new FunctionSymbol(functionDeclarationStatement.identifier.Text,
			typeParameterSymbols, parameters, statement.scope)
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

	public void Visit(CollectedExternalFunctionStatement statement)
	{
		
		var externalFunctionStatement = statement.externalFunctionStatement;

		var parameters = new List<ParameterSymbol>();
		var requireDefault = false;
		var additionalParametersAllowed = true;
		foreach (var parameter in externalFunctionStatement.parameters)
		{
			if (!additionalParametersAllowed)
				exceptions.Add(NewCollectionException("Cannot define additional parameters after a " +
				                                      "variadic parameter", parameter.identifier));
			
			var varSymbol = new VarSymbol(parameter.identifier.Text, parameter.isMutable);

			if (parameter.type is not null)
			{
				var invalidTypes = new List<Token>();
				varSymbol.EvaluatedType = ResolveType(parameter.type, invalidTypes);

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
					
					if (varSymbol.EvaluatedType is not null && varSymbol.EvaluatedType is not ArrayType)
						exceptions.Add(NewCollectionException("Variadic parameters must be array types",
							parameter.identifier));
				}
			}

			if (parameter.defaultExpression is { } defaultExpression)
			{
				requireDefault = true;
				parameters.Add(new ParameterSymbol(varSymbol, defaultExpression, parameter.isVariadic,
					(uint)parameters.Count));
				
				continue;
			}
			
			if (requireDefault)
				exceptions.Add(NewCollectionException(
					$"Parameter '{parameter.identifier.Text}' requires a default value because a " +
					"previous parameter specified a default value", parameter.identifier));

			parameters.Add(new ParameterSymbol(varSymbol, null, parameter.isVariadic, (uint)parameters.Count));
		}

		Type? returnType = null;
		if (externalFunctionStatement.returnType is not null)
		{
			var invalidTypes = new List<Token>();
			returnType = ResolveType(externalFunctionStatement.returnType, invalidTypes);
			
			if (invalidTypes.Count > 0)
			{
				foreach (var invalidType in invalidTypes)
				{
					exceptions.Add(NewCollectionException(
						$"Could not find a type named '{invalidType.Text}'", invalidType));
				}
			}
		}

		var externalFunctionSymbol = new ExternalFunctionSymbol(externalFunctionStatement.identifier.Text, parameters,
			externalFunctionStatement.attributes)
		{
			ReturnType = returnType
		};

		statement.externalFunctionSymbol = externalFunctionSymbol;

		if (CurrentScope.LookupSymbol(externalFunctionSymbol.Name) is ExternalFunctionSymbol existingFunctionSymbol)
		{
			if (externalFunctionSymbol.SignatureMatches(existingFunctionSymbol))
				throw NewCollectionException("External function signature matches existing external function signature",
					externalFunctionStatement.identifier);
		}
		else
		{
			CurrentScope.AddSymbol(externalFunctionSymbol);
		}
	}

	public void Visit(CollectedConstructorDeclarationStatement statement)
	{
		var constructorDeclarationStatement = statement.constructorDeclarationStatement;
		scopeStack.Push(statement.scope);

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
				varSymbol.EvaluatedType = ResolveType(parameter.type, invalidTypes);

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
					
					if (varSymbol.EvaluatedType is not null && varSymbol.EvaluatedType is not ArrayType)
						exceptions.Add(NewCollectionException("Variadic parameters must be array types",
							parameter.identifier));
				}
			}

			if (parameter.defaultExpression is { } defaultExpression)
			{
				requireDefault = true;
				parameters.Add(new ParameterSymbol(varSymbol, defaultExpression, parameter.isVariadic,
					(uint)parameters.Count));
				
				continue;
			}
			
			if (requireDefault)
				exceptions.Add(NewCollectionException(
					$"Parameter '{parameter.identifier.Text}' requires a default value because a " +
					"previous parameter specified a default value", parameter.identifier));

			parameters.Add(new ParameterSymbol(varSymbol, null, parameter.isVariadic, (uint)parameters.Count));
		}

		foreach (var parameter in parameters)
		{
			statement.scope.AddSymbol(parameter);
		}
		
		// Intrinsic 'this'
		var thisSymbol = new ParameterSymbol(new VarSymbol("this", false)
		{
			EvaluatedType = new ReferenceType(new BaseType(CurrentType), false)
		}, null, false, 0u);
		statement.scope.AddSymbol(thisSymbol);

		var constructorSymbol = new ConstructorSymbol(CurrentType, thisSymbol, parameters, statement.scope);

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
		scopeStack.Push(statement.scope);

		var destructorSymbol = new DestructorSymbol(statement.scope);

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

	public void Visit(CollectedStringDeclarationStatement statement)
	{
		var stringDeclarationStatement = statement.stringDeclarationStatement;
		scopeStack.Push(statement.scope);
		
		// Intrinsic 'this'
		var thisSymbol = new ParameterSymbol(new VarSymbol("this", false)
		{
			EvaluatedType = new ReferenceType(new BaseType(CurrentType), false)
		}, null, false, 0u);
		statement.scope.AddSymbol(thisSymbol);

		var stringFunctionSymbol = new StringFunctionSymbol(CurrentType, thisSymbol, statement.scope);

		statement.stringFunctionSymbol = stringFunctionSymbol;
		scopeStack.Pop();

		if (CurrentScope.LookupSymbol(stringFunctionSymbol.Name) is StringFunctionSymbol)
		{
			exceptions.Add(NewCollectionException("A string function is already declared in this scope",
				stringDeclarationStatement.stringKeyword));

			return;
		}
		
		CurrentScope.AddSymbol(stringFunctionSymbol);
	}

	public void Visit(CollectedCastDeclarationStatement statement)
	{
		var castDeclarationStatement = statement.castDeclarationStatement;

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
			varSymbol.EvaluatedType = ResolveType(parameter.type, invalidTypes);

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
				
		var parameterSymbol = new ParameterSymbol(varSymbol, null, false, 0u);
		statement.scope.AddSymbol(parameterSymbol);

		var castSymbol = new CastOverloadSymbol(castDeclarationStatement.isImplicit, parameterSymbol,
			returnType, statement.scope);

		if (CurrentScope.LookupSymbol(castSymbol.Name) is not null)
		{
			exceptions.Add(NewCollectionException("Cast overload matches existing cast overload",
				castDeclarationStatement.castKeyword));
			
			return;
		}

		CurrentScope.AddSymbol(castSymbol);
		statement.castOverloadSymbol = castSymbol;
	}

	public void Visit(CollectedOperatorDeclarationStatement statement)
	{
		var operatorDeclarationStatement = statement.operatorDeclarationStatement;
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
					leftSymbol.EvaluatedType = ResolveType(left.type, invalidTypes);

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
					rightSymbol.EvaluatedType = ResolveType(right.type, invalidTypes);

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
				
				var leftParameterSymbol = new ParameterSymbol(leftSymbol, null, false, 0u);
				var rightParameterSymbol = new ParameterSymbol(rightSymbol, null, false, 1u);
				statement.scope.AddSymbol(leftParameterSymbol);
				statement.scope.AddSymbol(rightParameterSymbol);

				var operatorSymbol = new BinaryOperatorOverloadSymbol(leftParameterSymbol,
					binaryOperationExpression.operation, rightParameterSymbol, returnType, statement.scope, false);

				if (CurrentScope.LookupSymbol(operatorSymbol.Name) is not null)
				{
					exceptions.Add(NewCollectionException("Operator overload matches existing operator overload",
						operatorDeclarationStatement.operatorKeyword));
					
					break;
				}

				CurrentScope.AddSymbol(operatorSymbol);
				CurrentType.Operators.Add(operatorSymbol);
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
					varSymbol.EvaluatedType = ResolveType(operand.type, invalidTypes);

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
				
				var parameterSymbol = new ParameterSymbol(varSymbol, null, false, 0u);
				statement.scope.AddSymbol(parameterSymbol);
				CurrentScope.AddSymbol(parameterSymbol);

				var operatorSymbol = new UnaryOperatorOverloadSymbol(parameterSymbol,
					unaryOperationExpression.operation, returnType, statement.scope, false);

				if (CurrentScope.LookupSymbol(operatorSymbol.Name) is not null)
				{
					exceptions.Add(NewCollectionException("Operator overload matches existing operator overload",
						operatorDeclarationStatement.operatorKeyword));
					
					break;
				}

				CurrentScope.AddSymbol(operatorSymbol);
				CurrentType.Operators.Add(operatorSymbol);
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

	public void Visit(CollectedExpressionStatement statement)
	{
		// Do nothing
	}

	public void Visit(CollectedBlockStatement statement)
	{
		// Do nothing
	}

	public void Visit(CollectedVarDeclarationStatement statement)
	{
		// Do nothing
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
				
				return new FunctionType(lambdaParameterTypes, returnType);

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