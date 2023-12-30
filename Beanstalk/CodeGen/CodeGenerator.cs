using LLVMSharp.Interop;

namespace Beanstalk.CodeGen;

public static class CodeGenerator
{
	public static void Generate()
	{
		/*var bytes = "hello world"u8.ToArray();
		unsafe
		{
			fixed (byte* p = bytes)
			{
				var module = LLVM.ModuleCreateWithName((sbyte*)p);
				var builder = LLVM.CreateBuilder();
				
				LLVM.DumpModule(module);
			}
		}*/
	}
}