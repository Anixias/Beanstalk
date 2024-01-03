using System.Diagnostics;
using System.Reflection;
using System.Text;
using Beanstalk.Analysis.Semantics;
using LLVMSharp.Interop;

namespace Beanstalk.CodeGen;

public unsafe class CodeGenerator
{
	private const string LibCResource = "Beanstalk.libc.src.beanstalk.a";

	private LLVMOpaqueModule* currentModule;
	private LLVMOpaqueContext* currentContext;
	private LLVMOpaqueBuilder* currentBuilder;
	
	public void Generate(IEnumerable<ResolvedAst> asts, int optimizationLevel, string outputPath)
	{
		var sourceFiles = new List<string>();
		foreach (var ast in asts)
		{
			var outputDirectory = Path.GetDirectoryName(outputPath) + "/bin/";
			Directory.CreateDirectory(outputDirectory);
			
			var relativePath = Path.GetRelativePath(ast.WorkingDirectory, ast.FilePath);
			currentModule = LLVM.ModuleCreateWithName(ConvertString(relativePath));
			currentContext = LLVM.GetModuleContext(currentModule);
			currentBuilder = LLVM.CreateBuilderInContext(currentContext);
			
			/* === Entry Point Function ===
			var entryPoint = LLVM.AddFunction(currentModule, ConvertString("main"),
				LLVM.FunctionType(LLVM.Int32Type(), (LLVMOpaqueType**)IntPtr.Zero, 0u, 0));
			var entryBody = LLVM.AppendBasicBlockInContext(currentContext, entryPoint, ConvertString("entry"));
			LLVM.PositionBuilder(currentBuilder, entryBody, (LLVMOpaqueValue*)IntPtr.Zero);
			LLVM.BuildRet(currentBuilder, LLVM.ConstInt(LLVM.Int32Type(), 0, 1));
			if (LLVM.VerifyFunction(entryPoint, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
				LLVM.InstructionEraseFromParent(entryPoint);*/
			
			LLVM.DumpModule(currentModule);

			var outputBitCodePath = Path.Combine(outputDirectory, Path.ChangeExtension(relativePath, ".bc"));
			LLVM.WriteBitcodeToFile(currentModule, ConvertString(outputBitCodePath));
			sourceFiles.Add(outputBitCodePath);
		}

		Process process;
		var extension = Path.GetExtension(outputPath);
		switch (extension)
		{
			case ".dll":
			{
				var processStartInfo = new ProcessStartInfo
				{
					FileName = @"C:\Program Files\LLVM\bin\clang.exe",
					Arguments = $"{string.Join(' ', sourceFiles)} --shared --output={outputPath}",
					WindowStyle = ProcessWindowStyle.Hidden
				};

				process = new Process
				{
					StartInfo = processStartInfo
				};
			
				process.Start();
				process.WaitForExit();
			}
				break;

			case ".lib":
			{
				var objectFiles = new List<string>();
				foreach (var file in sourceFiles)
				{
					var objectFile = Path.ChangeExtension(file, ".o");
					objectFiles.Add(objectFile);
					
					var processStartInfo = new ProcessStartInfo
					{
						FileName = @"C:\Program Files\LLVM\bin\clang.exe",
						Arguments = $"{file} --compile --output={objectFile}",
						WindowStyle = ProcessWindowStyle.Hidden
					};

					process = new Process
					{
						StartInfo = processStartInfo
					};

					process.Start();
					process.WaitForExit();
				}
				
				var llvmArStartInfo = new ProcessStartInfo
				{
					FileName = @"C:\Program Files\LLVM\bin\llvm-ar.exe",
					Arguments = $"rc {outputPath} {string.Join(' ', objectFiles)}",
					WindowStyle = ProcessWindowStyle.Hidden
				};

				process = new Process
				{
					StartInfo = llvmArStartInfo
				};

				process.Start();
				process.WaitForExit();
			}
				break;

			case ".exe":
			{
				var processStartInfo = new ProcessStartInfo
				{
					FileName = @"C:\Program Files\LLVM\bin\clang.exe",
					Arguments = $"{string.Join(' ', sourceFiles)} --output={outputPath}",
					WindowStyle = ProcessWindowStyle.Hidden
				};

				process = new Process
				{
					StartInfo = processStartInfo
				};
			
				process.Start();
				process.WaitForExit();
			}
				break;
			
			default:
				throw new Exception("Unsupported output type");
		}
	}

	private static sbyte* ConvertString(string text)
	{
		var bytes = Encoding.Default.GetBytes(text);
		fixed (byte* p = bytes)
		{
			return (sbyte*)p;
		}
	}

	private static void PadToMultipleOf(ref byte[] src, int pad)
	{
		var length = (src.Length + pad - 1) / pad * pad;
		Array.Resize(ref src, length);
	}

	private static Stream? OpenLibC()
	{
		var assembly = Assembly.GetExecutingAssembly();
		return assembly.GetManifestResourceStream(LibCResource);
	}

	private static LLVMOpaqueType* GetNativeType(NativeSymbol nativeSymbol)
	{
		if (nativeSymbol == TypeSymbol.Int8)
			return LLVM.Int8Type();
		
		if (nativeSymbol == TypeSymbol.Int16)
			return LLVM.Int16Type();
		
		if (nativeSymbol == TypeSymbol.Int32)
			return LLVM.Int32Type();
		
		if (nativeSymbol == TypeSymbol.Int64)
			return LLVM.Int64Type();
		
		if (nativeSymbol == TypeSymbol.Int128)
			return LLVM.Int128Type();
		
		// LLVM Does not distinguish between signed and unsigned types except for via instructions generated
		if (nativeSymbol == TypeSymbol.UInt8)
			return LLVM.Int8Type();
		
		if (nativeSymbol == TypeSymbol.UInt16)
			return LLVM.Int16Type();
		
		if (nativeSymbol == TypeSymbol.UInt32)
			return LLVM.Int32Type();
		
		if (nativeSymbol == TypeSymbol.UInt64)
			return LLVM.Int64Type();
		
		if (nativeSymbol == TypeSymbol.UInt128)
			return LLVM.Int128Type();
		
		if (nativeSymbol == TypeSymbol.Float32)
			return LLVM.FloatType();
		
		if (nativeSymbol == TypeSymbol.Float64)
			return LLVM.DoubleType();
		
		if (nativeSymbol == TypeSymbol.Float128)
			return LLVM.FP128Type();
		
		// Todo: Handle fixed point types
		
		if (nativeSymbol == TypeSymbol.Bool)
			return LLVM.Int1Type();
		
		if (nativeSymbol == TypeSymbol.Char)
			return LLVM.Int16Type();
		
		// Todo: This is probably not correct
		if (nativeSymbol == TypeSymbol.String)
			return LLVM.ArrayType(GetNativeType(TypeSymbol.Char), 0u);

		return (LLVMOpaqueType*)IntPtr.Zero;
	}
}