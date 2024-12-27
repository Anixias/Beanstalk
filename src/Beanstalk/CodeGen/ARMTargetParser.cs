using System.Diagnostics.CodeAnalysis;

namespace Beanstalk.CodeGen;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "CommentTypo")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
internal static class ARMTargetParser
{
	internal enum EndianKind
	{
		Invalid,
		Little,
		Big
	}

	internal enum ISAKind
	{
		Invalid,
		ARM,
		THUMB,
		AARCH64
	}
	
	internal static ISAKind ParseArchISA(string arch)
	{
		if (arch.StartsWith("aarch64"))
			return ISAKind.AARCH64;
		
		if (arch.StartsWith("arm64"))
			return ISAKind.AARCH64;
		
		if (arch.StartsWith("thumb"))
			return ISAKind.THUMB;
		
		if (arch.StartsWith("arm"))
			return ISAKind.ARM;

		return ISAKind.Invalid;
	}
	
	internal static EndianKind ParseArchEndian(string arch)
	{
		if (arch.StartsWith("armeb") || arch.StartsWith("thumbeb") || arch.StartsWith("aarch64_be"))
			return EndianKind.Big;

		if (arch.StartsWith("arm") || arch.StartsWith("thumb"))
			return arch.EndsWith("eb") ? EndianKind.Big : EndianKind.Little;

		if (arch.StartsWith("aarch64") || arch.StartsWith("aarch64_32"))
			return EndianKind.Little;

		return EndianKind.Invalid;
	}

	internal static string GetCanonicalArchName(string arch)
	{
		int offset;
		var a = arch;

		if (a.StartsWith("aarch64"))
		{
			if (a.Contains("eb"))
				return "";
			
			offset = 7;
			if (a.Substring(offset, 3) == "_be")
				offset += 3;
		}
		else
		{
			offset = GetOffset("arm64_32", "arm64e", "arm64", "aarch64_32", "arm", "thumb");
		}

		if (offset != 0 && a.Substring(offset, 2) == "eb")
			offset += 2;
		else if (a.EndsWith("eb"))
			a = a[..^2];

		if (offset > 0)
			a = a[offset..];

		if (a == "")
			return arch;

		if (offset == 0)
			return a;
		
		if (a.Length >= 2 && (a[0] != 'v' || !char.IsDigit(a[1])))
			return "";

		if (a.Contains("eb"))
			return "";

		return a;

		int GetOffset(params string[] names)
		{
			foreach (var name in names)
			{
				if (arch.StartsWith(name))
					return name.Length;
			}

			return 0;
		}
	}
}