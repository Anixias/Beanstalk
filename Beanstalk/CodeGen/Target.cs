namespace Beanstalk.CodeGen;

public sealed class Target
{
	internal readonly Triple triple;

	public Target(string triple)
	{
		this.triple = new Triple(triple);
	}

	public bool Is16Bit()
	{
		return triple.IsArch16Bit();
	}

	public bool Is32Bit()
	{
		return triple.IsArch32Bit();
	}

	public bool Is64Bit()
	{
		return triple.IsArch64Bit();
	}
}