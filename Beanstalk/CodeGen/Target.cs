using System.Runtime.CompilerServices;

namespace Beanstalk.CodeGen;

public sealed class Target
{
	internal readonly Triple triple;

	public Target(string triple)
	{
		this.triple = new Triple(triple);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is16Bit()
	{
		return triple.IsArch16Bit();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is32Bit()
	{
		return triple.IsArch32Bit();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is64Bit()
	{
		return triple.IsArch64Bit();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint PointerSize()
	{
		return Is64Bit() ? 64u : Is32Bit() ? 32u : 16u;
	}
}