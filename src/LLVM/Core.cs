// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

// ReSharper disable InvalidXmlDocComment
namespace LLVM;

public static unsafe class Core
{
	internal enum Opcode
	{
		/* Terminator Instructions */
		Ret            = 1,
		Br             = 2,
		Switch         = 3,
		IndirectBr     = 4,
		Invoke         = 5,
		/* removed 6 due to API changes */
		Unreachable    = 7,
		CallBr         = 67,

		/* Standard Unary Operators */
		FNeg           = 66,

		/* Standard Binary Operators */
		Add            = 8,
		FAdd           = 9,
		Sub            = 10,
		FSub           = 11,
		Mul            = 12,
		FMul           = 13,
		UDiv           = 14,
		SDiv           = 15,
		FDiv           = 16,
		URem           = 17,
		SRem           = 18,
		FRem           = 19,

		/* Logical Operators */
		Shl            = 20,
		LShr           = 21,
		AShr           = 22,
		And            = 23,
		Or             = 24,
		Xor            = 25,

		/* Memory Operators */
		Alloca         = 26,
		Load           = 27,
		Store          = 28,
		GetElementPtr  = 29,

		/* Cast Operators */
		Trunc          = 30,
		ZExt           = 31,
		SExt           = 32,
		FPToUI         = 33,
		FPToSI         = 34,
		UIToFP         = 35,
		SIToFP         = 36,
		FPTrunc        = 37,
		FPExt          = 38,
		PtrToInt       = 39,
		IntToPtr       = 40,
		BitCast        = 41,
		AddrSpaceCast  = 60,

		/* Other Operators */
		ICmp           = 42,
		FCmp           = 43,
		PHI            = 44,
		Call           = 45,
		Select         = 46,
		UserOp1        = 47,
		UserOp2        = 48,
		VAArg          = 49,
		ExtractElement = 50,
		InsertElement  = 51,
		ShuffleVector  = 52,
		ExtractValue   = 53,
		InsertValue    = 54,
		Freeze         = 68,

		/* Atomic operators */
		Fence          = 55,
		AtomicCmpXchg  = 56,
		AtomicRMW      = 57,

		/* Exception Handling Operators */
		Resume         = 58,
		LandingPad     = 59,
		CleanupRet     = 61,
		CatchRet       = 62,
		CatchPad       = 63,
		CleanupPad     = 64,
		CatchSwitch    = 65
	}

	internal enum TypeKind
	{
		/// <summary>
		/// Type with no size
		/// </summary>
		Void,
		
		/// <summary>
		/// 16-bit floating point type
		/// </summary>
		Half,
		
		/// <summary>
		/// 32-bit floating point type
		/// </summary>
		Float,
		
		/// <summary>
		/// 64-bit floating point type
		/// </summary>
		Double,
		
		/// <summary>
		/// 80-bit floating point type (x87)
		/// </summary>
		x86_FP80,
		
		/// <summary>
		/// 128-bit floating point type (112-bit mantissa)
		/// </summary>
		FP128,
		
		/// <summary>
		/// 128-bit floating point type (two 64-bits)
		/// </summary>
		PPC_FP128,
		
		/// <summary>
		/// Labels
		/// </summary>
		Label,
		
		/// <summary>
		/// Arbitrary bit width integers
		/// </summary>
		Integer,
		
		/// <summary>
		/// Functions
		/// </summary>
		Function,
		
		/// <summary>
		/// Structures
		/// </summary>
		Struct,
		
		/// <summary>
		/// Arrays
		/// </summary>
		Array,
		
		/// <summary>
		/// Pointers
		/// </summary>
		Pointer,
		
		/// <summary>
		/// Fixed width SIMD vector type
		/// </summary>
		Vector,
		
		/// <summary>
		/// Metadata
		/// </summary>
		Metadata,
		
		/// <summary>
		/// x86 MMX
		/// </summary>
		x86_MMX,
		
		/// <summary>
		/// Tokens
		/// </summary>
		Token,
		
		/// <summary>
		/// Scalable SIMD vector type
		/// </summary>
		ScalableVector,
		
		/// <summary>
		/// 16-bit brain floating point type
		/// </summary>
		BFloat,
		
		/// <summary>
		/// x86 AMX
		/// </summary>
		x86_AMX,
		
		/// <summary>
		/// Target extension type
		/// </summary>
		TargetExt,
	}

	internal enum Linkage
	{
		/// <summary>
		/// Externally visible function
		/// </summary>
		External,
		
		AvailableExternally,
		
		/// <summary>
		/// Keep one copy of function when linking (inline)
		/// </summary>
		LinkOnceAny,
		
		/// <summary>
		/// Keep one copy of function when linking (inline), but only replaced by something equivalent
		/// </summary>
		LinkOnceODR,
		
		/// <summary>
		/// Obsolete
		/// </summary>
		[Obsolete]
		LinkOnceODRAutoHide,
		
		/// <summary>
		/// Keep one copy of function when linking (weak)
		/// </summary>
		WeakAny,
		
		/// <summary>
		/// Keep one copy of function when linking (weak), but only replaced by something equivalent
		/// </summary>
		WeakODR,
		
		/// <summary>
		/// Special purpose, only applies to global arrays
		/// </summary>
		Appending,
		
		/// <summary>
		/// Rename collisions when linking (static functions)
		/// </summary>
		Internal,
		
		/// <summary>
		/// Like Internal, but omit from symbol table
		/// </summary>
		Private,
		
		/// <summary>
		/// Obsolete
		/// </summary>
		[Obsolete]
		DLLImport,
		
		/// <summary>
		/// Obsolete
		/// </summary>
		[Obsolete]
		DLLExport,
		
		ExternalWeak,
		
		/// <summary>
		/// Obsolete
		/// </summary>
		[Obsolete]
		Ghost,
		
		Common,
		
		/// <summary>
		/// Like Private, but linker removes
		/// </summary>
		LinkerPrivate,
		
		/// <summary>
		/// Like LinkerPrivate, but is weak
		/// </summary>
		LinkerPrivateWeak
	}

	internal enum Visibility
	{
		/// <summary>
		/// The GV is visible
		/// </summary>
		Default,
		
		/// <summary>
		/// The GV is hidden
		/// </summary>
		Hidden,
		
		/// <summary>
		/// The GV is protected
		/// </summary>
		Protected
	}

	internal enum UnnamedAddr
	{
		/// <summary>
		/// Address of the GV is significant
		/// </summary>
		No,
		
		/// <summary>
		/// Address of the GV is locally insignificant
		/// </summary>
		Local,
		
		/// <summary>
		/// Address of the GV is globally insignificant
		/// </summary>
		Global
	}

	internal enum DLLStorageClass
	{
		Default   = 0,
		
		/// <summary>
		/// Function to be imported from DLL
		/// </summary>
		DLLImport = 1,
		
		/// <summary>
		/// Function to be accessible from DLL
		/// </summary>
		DLLExport = 2
	}

	internal enum CallConv
	{
		C             = 0,
		Fast          = 8,
		Cold          = 9,
		GHC           = 10,
		HiPE          = 11,
		WebKitJS      = 12,
		AnyReg        = 13,
		PreserveMost  = 14,
		PreserveAll   = 15,
		Swift         = 16,
		CXXFASTTLS    = 17,
		x86Stdcall    = 64,
		x86Fastcall   = 65,
		ARMAPCS       = 66,
		ARMAAPCS      = 67,
		ARMAAPCSVFP   = 68,
		MSP430INTR    = 69,
		x86ThisCall   = 70,
		PTXKernel     = 71,
		PTXDevice     = 72,
		SPIRFUNC      = 75,
		SPIRKERNEL    = 76,
		IntelOCLBI    = 77,
		x8664SysV     = 78,
		Win64         = 79,
		x86VectorCall = 80,
		HHVM          = 81,
		HHVMC         = 82,
		x86INTR       = 83,
		AVRINTR       = 84,
		AVRSIGNAL     = 85,
		AVRBUILTIN    = 86,
		AMDGPUVS      = 87,
		AMDGPUGS      = 88,
		AMDGPUPS      = 89,
		AMDGPUCS      = 90,
		AMDGPUKERNEL  = 91,
		x86RegCall    = 92,
		AMDGPUHS      = 93,
		MSP430BUILTIN = 94,
		AMDGPULS      = 95,
		AMDGPUES      = 96
	}

	internal enum ValueKind
	{
		Argument,
		BasicBlock,
		MemoryUse,
		MemoryDef,
		MemoryPhi,

		Function,
		GlobalAlias,
		GlobalIFunc,
		GlobalVariable,
		BlockAddress,
		ConstantExpr,
		ConstantArray,
		ConstantStruct,
		ConstantVector,

		UndefValue,
		ConstantAggregateZero,
		ConstantDataArray,
		ConstantDataVector,
		ConstantInt,
		ConstantFP,
		ConstantPointerNull,
		ConstantTokenNone,

		MetadataAsValue,
		InlineAsm,

		Instruction,
		PoisonValue,
		ConstantTargetNone
	}
	
	internal enum RealPredicate
	{
		/// <summary>
		/// Always false (always folded)
		/// </summary>
		False,
		
		/// <summary>
		/// True if ordered and equal
		/// </summary>
		OEQ,
		
		/// <summary>
		/// True if ordered and greater than
		/// </summary>
		OGT,
		
		/// <summary>
		/// True if ordered and greater than or equal
		/// </summary>
		OGE,
		
		/// <summary>
		/// True if ordered and less than
		/// </summary>
		OLT,
		
		/// <summary>
		/// True if ordered and less than or equal
		/// </summary>
		OLE,
		
		/// <summary>
		/// True if ordered and operands are unequal
		/// </summary>
		ONE,
		
		/// <summary>
		/// True if ordered (no nans)
		/// </summary>
		ORD,
		
		/// <summary>
		/// True if unordered: isnan(X) | isnan(Y)
		/// </summary>
		UNO,
		
		/// <summary>
		/// True if unordered or equal
		/// </summary>
		UEQ,
		
		/// <summary>
		/// True if unordered or greater than
		/// </summary>
		UGT,
		
		/// <summary>
		/// True if unordered, greater than, or equal
		/// </summary>
		UGE,
		
		/// <summary>
		/// True if unordered or less than
		/// </summary>
		ULT,
		
		/// <summary>
		/// True if unordered, less than, or equal
		/// </summary>
		ULE,
		
		/// <summary>
		/// True if unordered or not equal
		/// </summary>
		UNE,
		
		/// <summary>
		/// Always true (always folded)
		/// </summary>
		True
	}

	internal enum LandingPadClauseType
	{
		/// <summary>
		/// A catch clause
		/// </summary>
		Catch,
		
		/// <summary>
		/// A filter clause
		/// </summary>
		Filter
	}

	internal enum ThreadLocalMode
	{
		NotThreadLocal = 0,
		GeneralDynamicTLSModel,
		LocalDynamicTLSModel,
		InitialExecTLSModel,
		LocalExecTLSModel
	}

	internal enum AtomicOrdering
	{
		/// <summary>
		/// A load or store which is not atomic
		/// </summary>
		NotAtomic = 0,
		
		/// <summary>
		/// Lowest level of atomicity, guarantees somewhat sane results, lock free
		/// </summary>
		Unordered = 1,
		
		/// <summary>
		/// Guarantees that if you take all the operations affecting a specific address, a consistent ordering exists
		/// </summary>
		Monotonic = 2,
		
		/// <summary>
		/// Acquire provides a barrier of the sort necessary to acquire a lock to access other memory with normal loads
		/// and stores
		/// </summary>
		Acquire = 4,
		
		/// <summary>
		/// Release is similar to Acquire, but with a barrier of the sort necessary to release a lock
		/// </summary>
		Release = 5,
		
		/// <summary>
		/// Provides both an Acquire and a Release barrier (for fences and operations which both read and write memory)
		/// </summary>
		AcquireRelease = 6,
		
		/// <summary>
		/// Provides Acquire semantics for loads and Release semantics for stores. Additionally, it guarantees that a
		/// total ordering exists between all SequentiallyConsistent operations
		/// </summary>
		SequentiallyConsistent = 7
	}

	internal enum AtomicRMWBinOp
	{
		/// <summary>
		/// Set the new value and return the one old
		/// </summary>
		Xchg,
		
		/// <summary>
		/// Add a value and return the old one
		/// </summary>
		Add,
		
		/// <summary>
		/// Subtract a value and return the old one
		/// </summary>
		Sub,
		
		/// <summary>
		/// AND a value and return the old one
		/// </summary>
		And,
		
		/// <summary>
		/// NOT-AND a value and return the old one
		/// </summary>
		Nand,
		
		/// <summary>
		/// OR a value and return the old one
		/// </summary>
		Or,
		
		/// <summary>
		/// XOR a value and return the old one
		/// </summary>
		Xor,
		
		/// <summary>
		/// Sets the value if it's greater than the original using a signed comparison and return the old one
		/// </summary>
		Max,
		
		/// <summary>
		/// Sets the value if it's less than the original using a signed comparison and return the old one
		/// </summary>
		Min,
		
		/// <summary>
		/// Sets the value if it's greater than the original using an unsigned comparison and return the old one
		/// </summary>
		UMax,
		
		/// <summary>
		/// Sets the value if it's less than the original using an unsigned comparison and return the old one
		/// </summary>
		UMin,
		
		/// <summary>
		/// Add a floating point value and return the old one
		/// </summary>
		FAdd,
		
		/// <summary>
		/// Subtract a floating point value and return the old one
		/// </summary>
		FSub,
		
		/// <summary>
		/// Sets the value if it's greater than the original using a floating point comparison and return the old one
		/// </summary>
		FMax,
		
		/// <summary>
		/// Sets the value if it's less than the original using a floating point comparison and return the old one
		/// </summary>
		FMin
	}

	internal enum DiagnosticSeverity
	{
		Error,
		Warning,
		Remark,
		Note
	}

	internal enum InlineAsmDialect
	{
		ATT,
		Intel
	}

	internal enum ModuleFlagBehavior
	{
		/**
		* Emits an error if two values disagree, otherwise the resulting value is
		* that of the operands.
		*
		* @see Module::ModFlagBehavior::Error
		*/
		Error,
		/**
		* Emits a warning if two values disagree. The result value will be the
		* operand for the flag from the first module being linked.
		*
		* @see Module::ModFlagBehavior::Warning
		*/
		Warning,
		/**
		* Adds a requirement that another module flag be present and have a
		* specified value after linking is performed. The value must be a metadata
		* pair, where the first element of the pair is the ID of the module flag
		* to be restricted, and the second element of the pair is the value the
		* module flag should be restricted to. This behavior can be used to
		* restrict the allowable results (via triggering of an error) of linking
		* IDs with the **Override** behavior.
		*
		* @see Module::ModFlagBehavior::Require
		*/
		Require,
		/**
		* Uses the specified value, regardless of the behavior or value of the
		* other module. If both modules specify **Override**, but the values
		* differ, an error will be emitted.
		*
		* @see Module::ModFlagBehavior::Override
		*/
		Override,
		/**
		* Appends the two values, which are required to be metadata nodes.
		*
		* @see Module::ModFlagBehavior::Append
		*/
		Append,
		/**
		* Appends the two values, which are required to be metadata
		* nodes. However, duplicate entries in the second list are dropped
		* during the append operation.
		*
		* @see Module::ModFlagBehavior::AppendUnique
		*/
		AppendUnique,
	}

	/**
	 * Attribute index are either LLVMAttributeReturnIndex,
	 * LLVMAttributeFunctionIndex or a parameter number from 1 to N.
	 */
	internal enum AttributeIndex
	{
		Return = 0,
		/// ISO C restricts enumerator values to range of 'int'
		/// (4294967295 is too large)
		/// LLVMAttributeFunctionIndex = ~0U,
		Function = -1
	}
}