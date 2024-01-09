﻿using Beanstalk.Analysis.Semantics;
using LLVMSharp.Interop;
using Type = Beanstalk.Analysis.Semantics.Type;

// ReSharper disable IdentifierTypo

namespace Beanstalk.CodeGen;

public unsafe partial class CodeGenerator
{
	private LLVMOpaqueValue* BuildAlloca(Type type, string name)
	{
		return BuildAlloca(GetType(type), name, GetSize(type));
	}
	
	private LLVMOpaqueValue* BuildAlloca(LLVMOpaqueType* type, string name, uint alignment)
	{
		var allocation = LLVM.BuildAlloca(currentBuilder, type, ConvertString(name));
		
		if (alignment > 0u)
		{
			LLVM.SetAlignment(allocation, alignment);
		}

		return allocation;
	}

	private LLVMOpaqueValue* BuildStore(LLVMOpaqueValue* value, LLVMOpaqueValue* destPtr, uint alignment)
	{
		var store = LLVM.BuildStore(currentBuilder, value, destPtr);
		
		if (alignment > 0u)
		{
			LLVM.SetAlignment(store, alignment);
		}

		return store;
	}

	private LLVMOpaqueValue* BuildIntToPtr(LLVMOpaqueValue* value, LLVMOpaqueType* destType, string name,
		uint alignment)
	{
		var intToPtr = LLVM.BuildIntToPtr(currentBuilder, value, destType, ConvertString(name));
		
		if (alignment > 0u)
		{
			LLVM.SetAlignment(intToPtr, alignment);
		}

		return intToPtr;
	}

	private LLVMOpaqueValue* BuildCall(LLVMOpaqueValue* function, LLVMOpaqueValue*[] arguments, string name)
	{
		var functionType = LLVM.GlobalGetValueType(function);
		var args = ConvertArrayToPointer(arguments);
		var argc = (uint)arguments.Length;
		return LLVM.BuildCall2(currentBuilder, functionType, function, args, argc, ConvertString(name));
	}

	private LLVMOpaqueValue* BuildLoad(LLVMOpaqueType* type, LLVMOpaqueValue* sourcePtr, string name, uint alignment)
	{
		var load = LLVM.BuildLoad2(currentBuilder, type, sourcePtr, ConvertString(name));

		if (alignment > 0u)
		{
			LLVM.SetAlignment(load, alignment);
		}

		return load;
	}

	private void DeclareStruct(StructSymbol structSymbol)
	{
		var structType = LLVM.StructCreateNamed(currentContext,
			ConvertString(structSymbol.Name));

		OpaqueType opaqueType;
		if (structSymbol.HasStaticFields)
		{
			var boolType = LLVM.Int1TypeInContext(currentContext);
			var staticInitialized = LLVM.AddGlobal(currentModule, boolType,
				ConvertString($"{structSymbol.Name}.staticInitialized"));

			LLVM.SetInitializer(staticInitialized, LLVM.ConstInt(boolType, 0u, LLVMBool.False));
			opaqueType = new OpaqueType(structType, staticInitialized, 0u);
		}
		else
		{
			opaqueType = new OpaqueType(structType, 0u);
		}
				
		typeSymbols.Add(structSymbol, opaqueType);
	}

	private void DeclareExternalFunction(ExternalFunctionSymbol externalFunctionSymbol)
	{
		if (valueSymbols.ContainsKey(externalFunctionSymbol))
			return;
		
		var parameterList = externalFunctionSymbol.Parameters;
		var parameterTypes = new LLVMOpaqueType*[parameterList.Length];
		var isVariadic = LLVMBool.False;

		for (var i = 0; i < parameterTypes.Length; i++)
		{
			var parameter = parameterList[i];
			parameterTypes[i] = GetType(parameter.EvaluatedType!);

			if (parameter.IsVariadic)
				isVariadic = LLVMBool.True;
		}

		var paramTypes = ConvertArrayToPointer(parameterTypes);
		var returnType = GetType(externalFunctionSymbol.ReturnType);
	
		var functionType = LLVM.FunctionType(returnType, paramTypes, (uint)parameterTypes.Length, isVariadic);
		var name = externalFunctionSymbol.Attributes.GetValueOrDefault("entry", externalFunctionSymbol.Name);
		
		var externalFunction = LLVM.AddFunction(currentModule, ConvertString(name),
			functionType);
		
		valueSymbols.Add(externalFunctionSymbol, new OpaqueValue(externalFunction));
	
		// Verification
		if (LLVM.VerifyFunction(externalFunction, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(externalFunction);
	}

	private void ImportSymbols(SymbolTable symbols)
	{
		foreach (var importedSymbol in symbols.Values)
		{
			switch (importedSymbol)
			{
				default:
					throw new InvalidOperationException($"Unsupported imported symbol '{importedSymbol.Name}': " +
					                                    $"{importedSymbol.SymbolTypeName}");
				
				case ImportGroupingSymbol importGroupingSymbol:
					ImportSymbols(importGroupingSymbol.Symbols);
					break;
				
				case StructSymbol structSymbol:
					DeclareStruct(structSymbol);
					break;
				
				case FunctionSymbol functionSymbol:
					throw new NotImplementedException();
				
				case ExternalFunctionSymbol functionSymbol:
					DeclareExternalFunction(functionSymbol);
					break;
			}
		}
	}

	private LLVMOpaqueValue* BuildStructGEP(LLVMOpaqueType* structType, LLVMOpaqueValue* structRef, uint fieldIndex,
		string fieldName)
	{
		return LLVM.BuildStructGEP2(currentBuilder, structType, structRef, fieldIndex, ConvertString(fieldName));
	}
}