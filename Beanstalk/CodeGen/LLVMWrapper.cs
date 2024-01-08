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
}