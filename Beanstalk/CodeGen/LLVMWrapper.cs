using System.Diagnostics.CodeAnalysis;
using Beanstalk.Analysis.Semantics;
using LLVMSharp.Interop;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable IdentifierTypo

namespace Beanstalk.CodeGen;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal enum AttributeKind : uint
{
	AllocAlign = 1u,
	AllocatedPointer = 2u,
	AlwaysInline = 3u,
	BuiltIn = 4u,
	Cold = 5u,
	Convergent = 6u,
	DisableSanitizerInstrumentation = 7u,
	FnRetThunkExtern = 8u,
	Hot = 9u,
	ImmediateArg = 10u,
	InRegister = 11u,
	InlineHint = 12u,
	JumpTable = 13u,
	MinSize = 14u,
	MustProgress = 15u,
	Naked = 16u,
	Nest = 17u,
	NoAlias = 18u,
	NoBuiltIn = 19u,
	NoCallback = 20u,
	NoCapture = 21u,
	NoCFCheck = 22u,
	NoDuplicate = 23u,
	NoFree = 24u,
	NoImplicitFloat = 25u,
	NoInline = 26u,
	NoMerge = 27u,
	NoProfile = 28u,
	NoRecurse = 29u,
	NoRedZone = 30u,
	NoReturn = 31u,
	NoSanitizeBounds = 32u,
	NoSanitizeCoverage = 33u,
	NoSync = 34u,
	NoUndef = 35u,
	NoUnwind = 36u,
	NonLazyBind = 37u,
	NonNull = 38u,
	NullPointerIsValid = 39u,
	OptimizeForFuzzing = 40u,
	OptimizeSize = 41u,
	OptimizeNone = 42u,
	PreSplitCoroutine = 43u,
	ReadNone = 44u,
	ReadOnly = 45u,
	Returned = 46u,
	ReturnsTwice = 47u,
	SignExt = 48u,
	SafeStack = 49u,
	SanitizeAddress = 50u,
	SanitizeHWAddress = 51u,
	SanitizeMemTag = 52u,
	SanitizeMemory = 53u,
	SanitizeThread = 54u,
	ShadowCallStack = 55u,
	SkipProfile = 56u,
	Speculatable = 57u,
	SpeculativeLoadHardening = 58u,
	StackProtect = 59u,
	StackProtectReq = 60u,
	StackProtectStrong = 61u,
	StrictFP = 62u,
	SwiftAsync = 63u,
	SwiftError = 64u,
	SwiftSelf = 65u,
	WillReturn = 66u,
	WriteOnly = 67u,
	ZeroExt = 68u,
	ByRef = 69u,
	ByVal = 70u,
	ElementType = 71u,
	InAlloca = 72u,
	PreAllocated = 73u,
	SRet = 74u,
	
	/// <summary>
	/// Requires an int parameter value
	/// </summary>
	Align = 75u,
	
	/// <summary>
	/// Requires a string parameter value
	/// </summary>
	AllocKind = 76u,
	
	/// <summary>
	/// Requires two int parameter values
	/// </summary>
	AllocSize = 77u,
	
	/// <summary>
	/// Requires an int parameter value
	/// </summary>
	Dereferenceable = 78u,
	
	/// <summary>
	/// Requires an int parameter value
	/// </summary>
	DereferenceableOrNull = 79u,
	
	/// <summary>
	/// Requires some value (0uL = "none")
	/// </summary>
	Memory = 80u,
	
	/// <summary>
	/// Requires some value (0uL = "none")
	/// </summary>
	NoFPClass = 81u,
	
	/// <summary>
	/// Requires an int parameter value
	/// </summary>
	AlignStack = 82u,

	/// <summary>
	/// This is garbage data and should not be used
	/// </summary>
	BrokenValue = 83u,
	
	/// <summary>
	/// Requires two int parameter values
	/// </summary>
	VScaleRange = 84u
}

public unsafe partial class CodeGenerator
{
	private LLVMOpaqueValue* BuildAlloca(LLVMOpaqueType* type, string name, uint alignment)
	{
		var allocation = LLVM.BuildAlloca(currentBuilder, type, ConvertString(name));
		
		if (alignment > 0u)
		{
			LLVM.SetAlignment(allocation, alignment);
		}

		return allocation;
	}
	
	private LLVMOpaqueValue* BuildArrayAlloca(LLVMOpaqueType* type, LLVMOpaqueValue* count, string name)
	{
		return LLVM.BuildArrayAlloca(currentBuilder, type, count, ConvertString(name));
	}
	
	private LLVMOpaqueValue* BuildMalloc(LLVMOpaqueType* type, string name)
	{
		return LLVM.BuildMalloc(currentBuilder, type, ConvertString(name));
	}
	
	private LLVMOpaqueValue* BuildArrayMalloc(LLVMOpaqueType* type, LLVMOpaqueValue* count, string name)
	{
		return LLVM.BuildArrayMalloc(currentBuilder, type, count, ConvertString(name));
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
		var externalFunction = LLVM.AddFunction(currentModule, ConvertString(name), functionType);
		
		if (externalFunctionSymbol.DllImportSource is { } dllImportSource)
		{
			LLVM.SetDLLStorageClass(externalFunction, LLVMDLLStorageClass.LLVMDLLImportStorageClass);
		}
		
		valueSymbols.Add(externalFunctionSymbol, new OpaqueValue(externalFunction));
	
		// Verification
		if (LLVM.VerifyFunction(externalFunction, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(externalFunction);
	}

	private void DeclareFunction(FunctionSymbol functionSymbol)
	{
		if (valueSymbols.ContainsKey(functionSymbol))
			return;
		
		var parameterList = functionSymbol.Parameters;
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
		var returnType = GetType(functionSymbol.ReturnType);
	
		var functionType = LLVM.FunctionType(returnType, paramTypes, (uint)parameterTypes.Length, isVariadic);
		var name = functionSymbol.Name;
		
		var function = LLVM.AddFunction(currentModule, ConvertString(name),
			functionType);
		
		valueSymbols.Add(functionSymbol, new OpaqueValue(function));
	
		// Verification
		if (LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(function);
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
					DeclareFunction(functionSymbol);
					break;
				
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

	private LLVMOpaqueValue* BuildInBoundsGEP(LLVMOpaqueType* type, LLVMOpaqueValue* pointer,
		LLVMOpaqueValue*[] indices, string name)
	{
		var indexArray = ConvertArrayToPointer(indices);
		var indexCount = (uint)indices.LongLength;
		return LLVM.BuildInBoundsGEP2(currentBuilder, type, pointer, indexArray, indexCount, ConvertString(name));
	}

	private LLVMOpaqueValue* BuildMemCpy(LLVMOpaqueValue* dst, uint dstAlign, LLVMOpaqueValue* src, uint srcAlign,
		LLVMOpaqueValue* count)
	{
		return LLVM.BuildMemCpy(currentBuilder, dst, dstAlign, src, srcAlign, count);
	}

	private LLVMOpaqueValue* BuildMemSet(LLVMOpaqueValue* dst, uint dstAlign, LLVMOpaqueValue* value,
		LLVMOpaqueValue* count)
	{
		return LLVM.BuildMemSet(currentBuilder, dst, value, count, dstAlign);
	}

	private static void SetMetadata(LLVMOpaqueContext* context, LLVMOpaqueValue* value, AttributeKind attributeKind)
	{
		var nullV = ConvertArrayToPointer(new LLVMOpaqueMetadata*[] {});
		var node = LLVM.MDNodeInContext2(context, nullV, 0u);
		LLVM.SetMetadata(value, (uint)attributeKind, LLVM.MetadataAsValue(context, node));
	}

	private static void GlobalSetMetadata(LLVMOpaqueContext* context, LLVMOpaqueValue* value, AttributeKind attributeKind)
	{
		var nullV = ConvertArrayToPointer(new LLVMOpaqueMetadata*[] {});
		var node = LLVM.MDNodeInContext2(context, nullV, 0u);
		LLVM.GlobalSetMetadata(value, (uint)attributeKind, node);
	}

	private static void SetFunctionAttribute(LLVMOpaqueContext* context, LLVMOpaqueValue* function,
		LLVMAttributeIndex index, AttributeKind attributeKind, ulong attributeValue = 0uL)
	{
		LLVM.AddAttributeAtIndex(function, index,
			LLVM.CreateEnumAttribute(context, (uint)attributeKind, attributeValue));
	}

	private static void SetFunctionParameterAttribute(LLVMOpaqueContext* context, LLVMOpaqueValue* function,
		uint parameterIndex, AttributeKind attributeKind, ulong attributeValue = 0uL)
	{
		LLVM.AddAttributeAtIndex(function, (LLVMAttributeIndex)(parameterIndex + 1u),
			LLVM.CreateEnumAttribute(context, (uint)attributeKind, attributeValue));
	}
}