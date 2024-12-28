using Beanstalk.Analysis.Semantics;
using LLVMSharp;
using LLVMSharp.Interop;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable IdentifierTypo

namespace Beanstalk.CodeGen;

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
		var nullV = ConvertArrayToPointer(new LLVMOpaqueMetadata*[] { });
		var node = LLVM.MDNodeInContext2(context, nullV, 0u);
		LLVM.SetMetadata(value, attributeKind.Convert(), LLVM.MetadataAsValue(context, node));
	}
	
	private static void GlobalSetMetadata(LLVMOpaqueContext* context, LLVMOpaqueValue* value,
		AttributeKind attributeKind)
	{
		var nullV = ConvertArrayToPointer(new LLVMOpaqueMetadata*[] { });
		var node = LLVM.MDNodeInContext2(context, nullV, 0u);
		LLVM.GlobalSetMetadata(value, attributeKind.Convert(), node);
	}
	
	private static void SetFunctionAttribute(LLVMOpaqueContext* context, LLVMOpaqueValue* function,
		LLVMAttributeIndex index, AttributeKind attributeKind, ulong attributeValue = 0uL)
	{
		LLVM.AddAttributeAtIndex(function, index,
			LLVM.CreateEnumAttribute(context, attributeKind.Convert(), attributeValue));
	}
	
	private static void SetFunctionParameterAttribute(LLVMOpaqueContext* context, LLVMOpaqueValue* function,
		uint parameterIndex, AttributeKind attributeKind, ulong attributeValue = 0uL)
	{
		var attr = LLVM.CreateEnumAttribute(context, attributeKind.Convert(), attributeValue);
		LLVM.AddAttributeAtIndex(function, (LLVMAttributeIndex)(parameterIndex + 1u), attr);
		var k = LLVM.GetEnumAttributeKind(attr);
		var v = LLVM.GetEnumAttributeValue(attr);
		var x = LLVM.GetEnumAttributeKindForName(ConvertString("noundef"), (nuint)"noundef".Length);
	}
}

internal static unsafe class LLVMExtensions
{
	private static readonly Dictionary<AttributeKind, uint> attributeKindLookup = [];
	
	internal static uint Convert(this AttributeKind attributeKind)
	{
		if (attributeKindLookup.TryGetValue(attributeKind, out var cached))
			return cached;
		
		return attributeKindLookup[attributeKind] = LookupValue(attributeKind);
	}
	
	private static uint LookupValue(AttributeKind attributeKind)
	{
		var name = attributeKind switch
		{
			AttributeKind.AlwaysInline => "alwaysinline",
			AttributeKind.Builtin => "builtin",
			AttributeKind.Cold => "cold",
			AttributeKind.Convergent => "convergent",
			AttributeKind.DisableSanitizerInstrumentation => "disable_sanitizer_instrumentation",
			AttributeKind.FnRetThunkExtern => "fn_ret_thunk_extern",
			AttributeKind.Hot => "hot",
			AttributeKind.InlineHint => "inlinehint",
			AttributeKind.JumpTable => "jumptable",
			AttributeKind.Memory => "memory",
			AttributeKind.MinSize => "minsize",
			AttributeKind.Naked => "naked",
			AttributeKind.NoBuiltin => "nobuiltin",
			AttributeKind.NoCallback => "nocallback",
			// nodivergencesource
			AttributeKind.NoDuplicate => "noduplicate",
			AttributeKind.NoFree => "nofree",
			AttributeKind.NoImplicitFloat => "noimplicitfloat",
			AttributeKind.NoInline => "noinline",
			AttributeKind.NoMerge => "nomerge",
			AttributeKind.NonLazyBind => "nonlazybind",
			AttributeKind.NoProfile => "noprofile",
			AttributeKind.SkipProfile => "skipprofile",
			AttributeKind.NoRedZone => "noredzone",
			// indirect-tls-seg-refs
			AttributeKind.NoReturn => "noreturn",
			AttributeKind.NoRecurse => "norecurse",
			AttributeKind.WillReturn => "willreturn",
			AttributeKind.NoSync => "nosync",
			AttributeKind.NoUnwind => "nounwind",
			AttributeKind.NoSanitizeBounds => "nosanitize_bounds",
			AttributeKind.NoSanitizeCoverage => "nosanitize_coverage",
			AttributeKind.NullPointerIsValid => "null_pointer_is_valid",
			// optdebug
			AttributeKind.OptForFuzzing => "optforfuzzing",
			AttributeKind.OptimizeNone => "optnone",
			AttributeKind.OptimizeForSize => "optsize",
			AttributeKind.ReturnsTwice => "returns_twice",
			AttributeKind.SafeStack => "safestack",
			AttributeKind.SanitizeAddress => "sanitize_address",
			AttributeKind.SanitizeMemory => "sanitize_memory",
			AttributeKind.SanitizeThread => "sanitize_thread",
			AttributeKind.SanitizeHWAddress => "sanitize_hwaddress",
			AttributeKind.SanitizeMemTag => "sanitize_memtag",
			// sanitize_realtime
			// sanitize_realtime_blocking
			AttributeKind.SpeculativeLoadHardening => "speculative_load_hardening",
			AttributeKind.Speculatable => "speculatable",
			AttributeKind.StackProtect => "ssp",
			AttributeKind.StackProtectStrong => "sspstrong",
			AttributeKind.StackProtectReq => "sspreq",
			AttributeKind.StrictFP => "strictfp",
			AttributeKind.UWTable => "uwtable",
			AttributeKind.NoCfCheck => "nocf_check",
			AttributeKind.ShadowCallStack => "shadowcallstack",
			AttributeKind.MustProgress => "mustprogress",
			AttributeKind.VScaleRange => "vscale_range",
			// vector-function-abi-variant
			// no_sanitize_address
			// no_sanitize_hwaddress
			// sanitize_address_dyninit
			AttributeKind.ZExt => "zeroext",
			AttributeKind.SExt => "signext",
			// noext
			AttributeKind.InReg => "inreg",
			AttributeKind.ByVal => "byval",
			AttributeKind.ByRef => "byref",
			AttributeKind.Preallocated => "preallocated",
			AttributeKind.InAlloca => "inalloca",
			AttributeKind.StructRet => "sret",
			AttributeKind.ElementType => "elementtype",
			AttributeKind.Alignment => "align",
			AttributeKind.NoAlias => "noalias",
			AttributeKind.NoCapture => "nocapture",
			AttributeKind.Nest => "nest",
			AttributeKind.Returned => "returned",
			AttributeKind.NonNull => "nonnull",
			AttributeKind.Dereferenceable => "dereferenceable",
			AttributeKind.DereferenceableOrNull => "dereferenceable_or_null",
			AttributeKind.SwiftSelf => "swiftself",
			AttributeKind.SwiftAsync => "swiftasync",
			AttributeKind.SwiftError => "swifterror",
			AttributeKind.ImmArg => "immarg",
			AttributeKind.NoUndef => "noundef",
			// nofpclass
			AttributeKind.StackAlignment => "alignstack",
			AttributeKind.AllocAlign => "allocalign",
			AttributeKind.AllocatedPointer => "allocptr",
			AttributeKind.ReadNone => "readnone",
			AttributeKind.ReadOnly => "readonly",
			AttributeKind.WriteOnly => "writeonly",
			// writable
			// initializes
			// dead_on_unwind
			// range
			_ => null
		};
		
		if (name is null)
			return default;
		
		return LLVM.GetEnumAttributeKindForName(CodeGenerator.ConvertString(name), (nuint)name.Length);
	}
}