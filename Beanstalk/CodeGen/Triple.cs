using System.Diagnostics.CodeAnalysis;

namespace Beanstalk.CodeGen;

/// <summary>
/// Source: https://llvm.org/doxygen/Triple_8h_source.html and https://llvm.org/doxygen/Triple_8cpp_source.html
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "CommentTypo")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
#pragma warning disable CS0660, CS0661
public struct Triple
#pragma warning restore CS0660, CS0661
{
	public enum ArchType
	{
		UnknownArch,

		/// ARM (little endian): arm, armv.*, xscale
		arm,
		/// ARM (big endian): armeb
		armeb,
		/// AArch64 (little endian): aarch64
		aarch64,
		/// AArch64 (big endian): aarch64_be
		aarch64_be,
		/// AArch64 (little endian) ILP32: aarch64_32
		aarch64_32,
		/// ARC: Synopsys ARC
		arc,
		/// AVR: Atmel AVR microcontroller
		avr,
		/// eBPF or extended BPF or 64-bit BPF (little endian)
		bpfel,
		/// eBPF or extended BPF or 64-bit BPF (big endian)
		bpfeb,
		/// CSKY: csky
		csky,
		/// DXIL 32-bit DirectX bytecode
		dxil,
		/// Hexagon: hexagon
		hexagon,
		/// LoongArch (32-bit): loongarch32
		loongarch32,
		/// LoongArch (64-bit): loongarch64
		loongarch64,
		/// M68k: Motorola 680x0 family
		m68k,
		/// MIPS: mips, mipsallegrex, mipsr6
		mips,
		/// MIPSEL: mipsel, mipsallegrexe, mipsr6el
		mipsel,
		/// MIPS64: mips64, mips64r6, mipsn32, mipsn32r6
		mips64,
		/// MIPS64EL: mips64el, mips64r6el, mipsn32el, mipsn32r6el
		mips64el,
		/// MSP430: msp430
		msp430,
		/// PPC: powerpc
		ppc,
		/// PPCLE: powerpc (little endian)
		ppcle,
		/// PPC64: powerpc64, ppu
		ppc64,
		/// PPC64LE: powerpc64le
		ppc64le,
		/// R600: AMD GPUs HD2XXX - HD6XXX
		r600,
		/// AMDGCN: AMD GCN GPUs
		amdgcn,
		/// RISC-V (32-bit): riscv32
		riscv32,
		/// RISC-V (64-bit): riscv64
		riscv64,
		/// Sparc: sparc
		sparc,
		/// Sparcv9: Sparcv9
		sparcv9,
		/// Sparc: (endianness = little). NB: 'Sparcle' is a CPU variant
		sparcel,
		/// SystemZ: s390x
		systemz,
		/// TCE (http://tce.cs.tut.fi/): tce
		tce,
		/// TCE little endian (http://tce.cs.tut.fi/): tcele
		tcele,
		/// Thumb (little endian): thumb, thumbv.*
		thumb,
		/// Thumb (big endian): thumbeb
		thumbeb,
		/// X86: i[3-9]86
		x86,
		/// X86-64: amd64, x86_64
		x86_64,
		/// XCore: xcore
		xcore,
		/// Tensilica: Xtensa
		xtensa,
		/// NVPTX: 32-bit
		nvptx,
		/// NVPTX: 64-bit
		nvptx64,
		/// le32: generic little-endian 32-bit CPU (PNaCl)
		le32,
		/// le64: generic little-endian 64-bit CPU (PNaCl)
		le64,
		/// AMDIL
		amdil,
		/// AMDIL with 64-bit pointers
		amdil64,
		/// AMD HSAIL
		hsail,
		/// AMD HSAIL with 64-bit pointers
		hsail64,
		/// SPIR: standard portable IR for OpenCL 32-bit version
		spir,
		/// SPIR: standard portable IR for OpenCL 64-bit version
		spir64,
		/// SPIR-V with logical memory layout.
		spirv,
		/// SPIR-V with 32-bit pointers
		spirv32,
		/// SPIR-V with 64-bit pointers
		spirv64,
		/// Kalimba: generic kalimba
		kalimba,
		/// SHAVE: Movidius vector VLIW processors
		shave,
		/// Lanai: Lanai 32-bit
		lanai,
		/// WebAssembly with 32-bit pointers
		wasm32,
		/// WebAssembly with 64-bit pointers
		wasm64,
		/// 32-bit RenderScript
		renderscript32,
		/// 64-bit RenderScript
		renderscript64,
		/// NEC SX-Aurora Vector Engine
		ve,
		LastArchType = ve
	}

	public enum SubArchType
	{
		NoSubArch,

		ARMSubArch_v9_5a,
		ARMSubArch_v9_4a,
		ARMSubArch_v9_3a,
		ARMSubArch_v9_2a,
		ARMSubArch_v9_1a,
		ARMSubArch_v9,
		ARMSubArch_v8_9a,
		ARMSubArch_v8_8a,
		ARMSubArch_v8_7a,
		ARMSubArch_v8_6a,
		ARMSubArch_v8_5a,
		ARMSubArch_v8_4a,
		ARMSubArch_v8_3a,
		ARMSubArch_v8_2a,
		ARMSubArch_v8_1a,
		ARMSubArch_v8,
		ARMSubArch_v8r,
		ARMSubArch_v8m_baseline,
		ARMSubArch_v8m_mainline,
		ARMSubArch_v8_1m_mainline,
		ARMSubArch_v7,
		ARMSubArch_v7em,
		ARMSubArch_v7m,
		ARMSubArch_v7s,
		ARMSubArch_v7k,
		ARMSubArch_v7ve,
		ARMSubArch_v6,
		ARMSubArch_v6m,
		ARMSubArch_v6k,
		ARMSubArch_v6t2,
		ARMSubArch_v5,
		ARMSubArch_v5te,
		ARMSubArch_v4t,

		AArch64SubArch_arm64e,
		AArch64SubArch_arm64ec,

		KalimbaSubArch_v3,
		KalimbaSubArch_v4,
		KalimbaSubArch_v5,

		MipsSubArch_r6,

		PPCSubArch_spe,

		// SPIR-V sub-arch corresponds to its version.
		SPIRVSubArch_v10,
		SPIRVSubArch_v11,
		SPIRVSubArch_v12,
		SPIRVSubArch_v13,
		SPIRVSubArch_v14,
		SPIRVSubArch_v15,
	}

	public enum VendorType
	{
		UnknownVendor,

		Apple,
		PC,
		SCEI,
		Freescale,
		IBM,
		ImaginationTechnologies,
		MipsTechnologies,
		NVIDIA,
		CSR,
		AMD,
		Mesa,
		SUSE,
		OpenEmbedded,
		LastVendorType = OpenEmbedded
	}

	public enum OSType
	{
		UnknownOS,

		Darwin,
		DragonFly,
		FreeBSD,
		Fuchsia,
		IOS,
		KFreeBSD,
		Linux,
		/// PS3
		Lv2,
		MacOSX,
		NetBSD,
		OpenBSD,
		Solaris,
		UEFI,
		Win32,
		ZOS,
		Haiku,
		RTEMS,
		/// Native Client
		NaCl,
		AIX,
		/// NVIDIA CUDA
		CUDA,
		/// NVIDIA OpenCL
		NVCL,
		/// AMD HSA Runtime
		AMDHSA,
		PS4,
		PS5,
		ELFIAMCU,
		/// Apple tvOS
		TvOS,
		/// Apple watchOS
		WatchOS,
		/// Apple DriverKit
		DriverKit,
		Mesa3D,
		/// AMD PAL Runtime
		AMDPAL,
		/// HermitCore Unikernel/Multikernel
		HermitCore,
		/// GNU/Hurd
		Hurd,
		/// Experimental WebAssembly OS
		WASI,
		Emscripten,
		/// DirectX ShaderModel
		ShaderModel,
		LiteOS,
		Serenity,
		LastOSType = Serenity
	}

	public enum EnvironmentType
	{
		UnknownEnvironment,

		GNU,
		GNUABIN32,
		GNUABI64,
		GNUEABI,
		GNUEABIHF,
		GNUF32,
		GNUF64,
		GNUSF,
		GNUX32,
		GNUILP32,
		CODE16,
		EABI,
		EABIHF,
		Android,
		Musl,
		MuslEABI,
		MuslEABIHF,
		MuslX32,

		MSVC,
		Itanium,
		Cygnus,
		CoreCLR,
		/// Simulator variants of other systems, e.g., Apple's iOS
		Simulator,
		/// Mac Catalyst variant of Apple's iOS deployment target.
		MacABI,

		// Shader Stages
		// The order of these values matters, and must be kept in sync with the
		// language options enum in Clang. The ordering is enforced in
		// static_asserts in Triple.cpp and in Clang.
		Pixel,
		Vertex,
		Geometry,
		Hull,
		Domain,
		Compute,
		Library,
		RayGeneration,
		Intersection,
		AnyHit,
		ClosestHit,
		Miss,
		Callable,
		Mesh,
		Amplification,

		OpenHOS,

		LastEnvironmentType = OpenHOS
	}

	public enum ObjectFormatType
	{
		UnknownObjectFormat,

		COFF,
		DXContainer,
		ELF,
		GOFF,
		MachO,
		SPIRV,
		Wasm,
		XCOFF,
	}

	private readonly string data = "";
	public ArchType Arch { get; set; }
	public SubArchType SubArch { get; set; }
	public VendorType Vendor { get; set; }
	public OSType OS { get; set; }
	public EnvironmentType Environment { get; set; }
	public ObjectFormatType ObjectFormat { get; set; }

	public Triple(string triple)
	{
		data = triple;
		var components = triple.Split('-');

		if (components.Length < 1)
			return;
		
		Arch = ParseArch(components[0]);
		//SubArch = ParseSubArch(components[0]);
		
		if (components.Length < 2)
			return;

		Vendor = ParseVendor(components[1]);
		
		if (components.Length < 3)
			return;

		OS = ParseOS(components[2]);
		
		if (components.Length < 4)
			return;

		Environment = ParseEnvironment(components[3]);
		ObjectFormat = ParseFormat(components[3]);
	}

	private static VendorType ParseVendor(string vendorName)
	{
		return vendorName switch
		{
			"apple" => VendorType.Apple,
			"pc" => VendorType.PC,
			"scei" => VendorType.SCEI,
			"sie" => VendorType.SCEI,
			"fsl" => VendorType.Freescale,
			"ibm" => VendorType.IBM,
			"img" => VendorType.ImaginationTechnologies,
			"mti" => VendorType.MipsTechnologies,
			"nvidia" => VendorType.NVIDIA,
			"csr" => VendorType.CSR,
			"amd" => VendorType.AMD,
			"mesa" => VendorType.Mesa,
			"suse" => VendorType.SUSE,
			"oe" => VendorType.OpenEmbedded,
			_ => VendorType.UnknownVendor
		};
	}

	private static OSType ParseOS(string osName)
	{
		if (osName.StartsWith("darwin"))
			return OSType.Darwin;
		
		if (osName.StartsWith("dragonfly"))
			return OSType.DragonFly;
		
		if (osName.StartsWith("freebsd"))
			return OSType.FreeBSD;
		
		if (osName.StartsWith("fuchsia"))
			return OSType.Fuchsia;
		
		if (osName.StartsWith("ios"))
			return OSType.IOS;
		
		if (osName.StartsWith("kfreebsd"))
			return OSType.KFreeBSD;
		
		if (osName.StartsWith("linux"))
			return OSType.Linux;
		
		if (osName.StartsWith("lv2"))
			return OSType.Lv2;
		
		if (osName.StartsWith("macos"))
			return OSType.MacOSX;
		
		if (osName.StartsWith("netbsd"))
			return OSType.NetBSD;
		
		if (osName.StartsWith("openbsd"))
			return OSType.OpenBSD;
		
		if (osName.StartsWith("solaris"))
			return OSType.Solaris;
		
		if (osName.StartsWith("uefi"))
			return OSType.UEFI;
		
		if (osName.StartsWith("win32") || osName.StartsWith("windows"))
			return OSType.Win32;
		
		if (osName.StartsWith("zos"))
			return OSType.ZOS;
		
		if (osName.StartsWith("haiku"))
			return OSType.Haiku;
		
		if (osName.StartsWith("rtems"))
			return OSType.RTEMS;
		
		if (osName.StartsWith("nacl"))
			return OSType.NaCl;
		
		if (osName.StartsWith("aix"))
			return OSType.AIX;
		
		if (osName.StartsWith("cuda"))
			return OSType.CUDA;
		
		if (osName.StartsWith("nvcl"))
			return OSType.NVCL;
		
		if (osName.StartsWith("amdhsa"))
			return OSType.AMDHSA;
		
		if (osName.StartsWith("ps4"))
			return OSType.PS4;
		
		if (osName.StartsWith("ps5"))
			return OSType.PS5;
		
		if (osName.StartsWith("elfiamcu"))
			return OSType.ELFIAMCU;
		
		if (osName.StartsWith("tvos"))
			return OSType.TvOS;
		
		if (osName.StartsWith("watchos"))
			return OSType.WatchOS;
		
		if (osName.StartsWith("driverkit"))
			return OSType.DriverKit;
		
		if (osName.StartsWith("mesa3d"))
			return OSType.Mesa3D;
		
		if (osName.StartsWith("amdpal"))
			return OSType.AMDPAL;
		
		if (osName.StartsWith("hermit"))
			return OSType.HermitCore;
		
		if (osName.StartsWith("hurd"))
			return OSType.Hurd;
		
		if (osName.StartsWith("wasi"))
			return OSType.WASI;
		
		if (osName.StartsWith("emscripten"))
			return OSType.Emscripten;
		
		if (osName.StartsWith("shadermodel"))
			return OSType.ShaderModel;
		
		if (osName.StartsWith("liteos"))
			return OSType.LiteOS;
		
		if (osName.StartsWith("serenity"))
			return OSType.Serenity;

		return OSType.UnknownOS;
	}

	private static EnvironmentType ParseEnvironment(string environmentName)
	{
		if (environmentName.StartsWith("eabihf"))
			return EnvironmentType.EABIHF;
		
		if (environmentName.StartsWith("eabi"))
			return EnvironmentType.EABI;
		
		if (environmentName.StartsWith("gnuabin32"))
			return EnvironmentType.GNUABIN32;
		
		if (environmentName.StartsWith("gnuabi64"))
			return EnvironmentType.GNUABI64;
		
		if (environmentName.StartsWith("gnueabihf"))
			return EnvironmentType.GNUEABIHF;
		
		if (environmentName.StartsWith("gnueabi"))
			return EnvironmentType.GNUEABI;
		
		if (environmentName.StartsWith("gnuf32"))
			return EnvironmentType.GNUF32;
		
		if (environmentName.StartsWith("gnuf64"))
			return EnvironmentType.GNUF64;
		
		if (environmentName.StartsWith("gnusf"))
			return EnvironmentType.GNUSF;
		
		if (environmentName.StartsWith("gnux32"))
			return EnvironmentType.GNUX32;
		
		if (environmentName.StartsWith("gnu_ilp32"))
			return EnvironmentType.GNUILP32;
		
		if (environmentName.StartsWith("code16"))
			return EnvironmentType.CODE16;
		
		if (environmentName.StartsWith("gnu"))
			return EnvironmentType.GNU;
		
		if (environmentName.StartsWith("android"))
			return EnvironmentType.Android;
		
		if (environmentName.StartsWith("musleabihf"))
			return EnvironmentType.MuslEABIHF;
		
		if (environmentName.StartsWith("musleabi"))
			return EnvironmentType.MuslEABI;

		if (environmentName.StartsWith("muslx32"))
			return EnvironmentType.MuslX32;

		if (environmentName.StartsWith("musl"))
			return EnvironmentType.Musl;

		if (environmentName.StartsWith("msvc"))
			return EnvironmentType.MSVC;

		if (environmentName.StartsWith("itanium"))
			return EnvironmentType.Itanium;

		if (environmentName.StartsWith("cygnus"))
			return EnvironmentType.Cygnus;

		if (environmentName.StartsWith("coreclr"))
			return EnvironmentType.CoreCLR;

		if (environmentName.StartsWith("simulator"))
			return EnvironmentType.Simulator;

		if (environmentName.StartsWith("macabi"))
			return EnvironmentType.MacABI;

		if (environmentName.StartsWith("pixel"))
			return EnvironmentType.Pixel;

		if (environmentName.StartsWith("vertex"))
			return EnvironmentType.Vertex;

		if (environmentName.StartsWith("geometry"))
			return EnvironmentType.Geometry;

		if (environmentName.StartsWith("hull"))
			return EnvironmentType.Hull;

		if (environmentName.StartsWith("domain"))
			return EnvironmentType.Domain;

		if (environmentName.StartsWith("compute"))
			return EnvironmentType.Compute;

		if (environmentName.StartsWith("library"))
			return EnvironmentType.Library;

		if (environmentName.StartsWith("raygeneration"))
			return EnvironmentType.RayGeneration;

		if (environmentName.StartsWith("intersection"))
			return EnvironmentType.Intersection;

		if (environmentName.StartsWith("anyhit"))
			return EnvironmentType.AnyHit;

		if (environmentName.StartsWith("closesthit"))
			return EnvironmentType.ClosestHit;

		if (environmentName.StartsWith("miss"))
			return EnvironmentType.Miss;

		if (environmentName.StartsWith("callable"))
			return EnvironmentType.Callable;

		if (environmentName.StartsWith("mesh"))
			return EnvironmentType.Mesh;

		if (environmentName.StartsWith("amplification"))
			return EnvironmentType.Amplification;

		if (environmentName.StartsWith("ohos"))
			return EnvironmentType.OpenHOS;

		return EnvironmentType.UnknownEnvironment;
	}

	private static ObjectFormatType ParseFormat(string formatName)
	{
		if (formatName.EndsWith("xcoff"))
			return ObjectFormatType.XCOFF;
		
		if (formatName.EndsWith("coff"))
			return ObjectFormatType.COFF;
		
		if (formatName.EndsWith("elf"))
			return ObjectFormatType.ELF;
		
		if (formatName.EndsWith("goff"))
			return ObjectFormatType.GOFF;
		
		if (formatName.EndsWith("macho"))
			return ObjectFormatType.MachO;
		
		if (formatName.EndsWith("wasm"))
			return ObjectFormatType.Wasm;
		
		if (formatName.EndsWith("spirv"))
			return ObjectFormatType.SPIRV;

		return ObjectFormatType.UnknownObjectFormat;
	}

	private static ArchType ParseArch(string archName)
	{
		var archType = Parse();
		if (archType != ArchType.UnknownArch)
			return archType;
		
		if (archName.StartsWith("arm") || archName.StartsWith("thumb") || archName.StartsWith("aarch64"))
			return ParseARMArch(archName);

		if (archName.StartsWith("bpf"))
			return ParseBPFArch(archName);

		return archType;
		
		ArchType Parse()
		{
			switch (archName)
			{
				case "i386":
				case "i486":
				case "i586":
				case "i686":
					return ArchType.x86;

				case "i786":
				case "i886":
				case "i986":
					return ArchType.x86;

				case "amd64":
				case "x86_64":
				case "x86_64h":
					return ArchType.x86_64;

				case "powerpc":
				case "powerpcspe":
				case "ppc":
				case "ppc32":
					return ArchType.ppc;

				case "powerpcle":
				case "ppcle":
				case "ppc32le":
					return ArchType.ppcle;

				case "powerpc64":
				case "ppu":
				case "ppc64":
					return ArchType.ppc64;

				case "powerpc64le":
				case "ppc64le":
					return ArchType.ppc64le;

				case "arm":
				case "xscale":
					return ArchType.arm;

				case "armeb":
				case "xscaleeb":
					return ArchType.armeb;

				case "aarch64":
					return ArchType.aarch64;

				case "aarch64_be":
					return ArchType.aarch64_be;

				case "aarch64_32":
					return ArchType.aarch64_32;

				case "arc":
					return ArchType.arc;

				case "arm64":
				case "arm64e":
				case "arm64ec":
					return ArchType.aarch64;

				case "arm64_32":
					return ArchType.aarch64_32;

				case "thumb":
					return ArchType.thumb;

				case "thumbeb":
					return ArchType.thumbeb;

				case "avr":
					return ArchType.avr;

				case "m68k":
					return ArchType.m68k;

				case "msp430":
					return ArchType.msp430;

				case "mips":
				case "mipseb":
				case "mipsallegrex":
				case "mipsisa32r6":
				case "mipsr6":
					return ArchType.mips;

				case "mipsel":
				case "mipsallgrexel":
				case "mipsisa32r6el":
				case "mipsr6el":
					return ArchType.mipsel;

				case "mips64":
				case "mips64eb":
				case "mipsn32":
				case "mipsisa64r6":
				case "mips64r6":
				case "mipsn32r6":
					return ArchType.mips64;

				case "mips64el":
				case "mipsn32el":
				case "mipsisa64r6el":
				case "mips64r6el":
				case "mipsn32r6el":
					return ArchType.mips64el;

				case "r600":
					return ArchType.r600;

				case "amdgcn":
					return ArchType.amdgcn;

				case "riscv32":
					return ArchType.riscv32;

				case "riscv64":
					return ArchType.riscv64;

				case "hexagon":
					return ArchType.hexagon;

				case "s390x":
				case "systemz":
					return ArchType.systemz;

				case "sparc":
					return ArchType.sparc;

				case "sparcel":
					return ArchType.sparcel;

				case "sparcv9":
				case "sparc64":
					return ArchType.sparcv9;

				case "tce":
					return ArchType.tce;

				case "tcele":
					return ArchType.tcele;

				case "xcore":
					return ArchType.xcore;

				case "nvptx":
					return ArchType.nvptx;

				case "nvptx64":
					return ArchType.nvptx64;

				case "le32":
					return ArchType.le32;

				case "le64":
					return ArchType.le64;

				case "amdil":
					return ArchType.amdil;

				case "amdil64":
					return ArchType.amdil64;

				case "hsail":
					return ArchType.hsail;

				case "hsail64":
					return ArchType.hsail64;

				case "spir":
					return ArchType.spir;

				case "spir64":
					return ArchType.spir64;

				case "spirv":
				case "spirv1.0":
				case "spirv1.1":
				case "spirv1.2":
				case "spirv1.3":
				case "spirv1.4":
				case "spirv1.5":
					return ArchType.spirv;

				case "spriv32":
				case "spirv32v1.0":
				case "spirv32v1.1":
				case "spirv32v1.2":
				case "spirv32v1.3":
				case "spirv32v1.4":
				case "spirv32v1.5":
					return ArchType.spirv32;

				case "spriv64":
				case "spirv64v1.0":
				case "spirv64v1.1":
				case "spirv64v1.2":
				case "spirv64v1.3":
				case "spirv64v1.4":
				case "spirv64v1.5":
					return ArchType.spirv32;
				
				case "lanai":
					return ArchType.lanai;
				
				case "renderscript32":
					return ArchType.renderscript32;
				
				case "renderscript64":
					return ArchType.renderscript64;
				
				case "shave":
					return ArchType.shave;
				
				case "ve":
					return ArchType.ve;
				
				case "wasm32":
					return ArchType.wasm32;
				
				case "wasm64":
					return ArchType.wasm64;
				
				case "csky":
					return ArchType.csky;
				
				case "loongarch32":
					return ArchType.loongarch32;
				
				case "loongarch64":
					return ArchType.loongarch64;
				
				case "dxil":
					return ArchType.dxil;
				
				case "xtensa":
					return ArchType.xtensa;

				default:
					return archName.StartsWith("kalimba") ? ArchType.kalimba : ArchType.UnknownArch;
			}
		}
	}

	public Triple(string ArchStr, string VendorStr, string OSStr)
	{
		
	}

	public Triple(string ArchStr, string VendorStr, string OSStr, string EnvironmentStr)
	{
		
	}

	public static bool operator ==(Triple left, Triple right)
	{
		if (left.Arch != right.Arch)
			return false;
		
		if (left.SubArch != right.SubArch)
			return false;
		
		if (left.Vendor != right.Vendor)
			return false;
		
		if (left.OS != right.OS)
			return false;
		
		if (left.Environment != right.Environment)
			return false;
		
		if (left.ObjectFormat != right.ObjectFormat)
			return false;

		return true;
	}

	public static bool operator !=(Triple left, Triple right)
	{
		return !(left == right);
	}

	private static uint GetArchPointerBitWidth(ArchType archType)
	{
		switch (archType)
		{
			default:
				throw new Exception("Invalid ArchType");
			
			case ArchType.UnknownArch:
				return 0u;
			
			case ArchType.avr:
			case ArchType.msp430:
				return 16u;
			
			case ArchType.aarch64_32:
			case ArchType.amdil:
			case ArchType.arc:
			case ArchType.arm:
			case ArchType.armeb:
			case ArchType.csky:
			case ArchType.dxil:
			case ArchType.hexagon:
			case ArchType.hsail:
			case ArchType.kalimba:
			case ArchType.lanai:
			case ArchType.le32:
			case ArchType.loongarch32:
			case ArchType.m68k:
			case ArchType.mips:
			case ArchType.mipsel:
			case ArchType.nvptx:
			case ArchType.ppc:
			case ArchType.ppcle:
			case ArchType.r600:
			case ArchType.renderscript32:
			case ArchType.riscv32:
			case ArchType.shave:
			case ArchType.sparc:
			case ArchType.sparcel:
			case ArchType.spir:
			case ArchType.spirv32:
			case ArchType.tce:
			case ArchType.tcele:
			case ArchType.thumb:
			case ArchType.thumbeb:
			case ArchType.wasm32:
			case ArchType.x86:
			case ArchType.xcore:
			case ArchType.xtensa:
				return 32u;
 
			case ArchType.aarch64:
			case ArchType.aarch64_be:
			case ArchType.amdgcn:
			case ArchType.amdil64:
			case ArchType.bpfeb:
			case ArchType.bpfel:
			case ArchType.hsail64:
			case ArchType.le64:
			case ArchType.loongarch64:
			case ArchType.mips64:
			case ArchType.mips64el:
			case ArchType.nvptx64:
			case ArchType.ppc64:
			case ArchType.ppc64le:
			case ArchType.renderscript64:
			case ArchType.riscv64:
			case ArchType.sparcv9:
			case ArchType.spirv:
			case ArchType.spir64:
			case ArchType.spirv64:
			case ArchType.systemz:
			case ArchType.ve:
			case ArchType.wasm64:
			case ArchType.x86_64:
				return 64u;
		}
	}

	public readonly bool IsArch64Bit() => GetArchPointerBitWidth(Arch) == 64u;
	public readonly bool IsArch32Bit() => GetArchPointerBitWidth(Arch) == 32u;
	public readonly bool IsArch16Bit() => GetArchPointerBitWidth(Arch) == 16u;

	public static string GetArchName(ArchType archType, SubArchType subArchType = SubArchType.NoSubArch)
	{
		return archType switch
		{
			ArchType.mips when subArchType is SubArchType.MipsSubArch_r6 => "mipsisa32r6",
			ArchType.mipsel when subArchType is SubArchType.MipsSubArch_r6 => "mipsisa32r6el",
			ArchType.mips64 when subArchType is SubArchType.MipsSubArch_r6 => "mipsisa64r6",
			ArchType.mips64el when subArchType is SubArchType.MipsSubArch_r6 => "mipsisa64r6el",
			ArchType.aarch64 when subArchType is SubArchType.AArch64SubArch_arm64ec => "arm64ec",
			ArchType.aarch64 when subArchType is SubArchType.AArch64SubArch_arm64e => "arm64e",
			_ => archType.ToString()
		};
	}

	public static string GetArchTypePrefix(ArchType archType)
	{
		switch (archType)
		{
			default:
				return "";
			
			case ArchType.aarch64:
			case ArchType.aarch64_be:
			case ArchType.aarch64_32:
				return "aarch64";
			
			case ArchType.arc:
				return "arc";
			
			case ArchType.arm:
			case ArchType.armeb:
			case ArchType.thumb:
			case ArchType.thumbeb:
				return "arm";
			
			case ArchType.avr:
				return "avr";
			
			case ArchType.ppc64:
			case ArchType.ppc64le:
			case ArchType.ppc:
			case ArchType.ppcle:
				return "ppc";
			
			case ArchType.m68k:
				return "m68k";
			
			case ArchType.mips:
			case ArchType.mipsel:
			case ArchType.mips64:
			case ArchType.mips64el:
				return "mips";
			
			case ArchType.hexagon:
				return "hexagon";
			
			case ArchType.amdgcn:
				return "amdgcn";
				
			case ArchType.r600:
				return "r600";
			
			case ArchType.bpfel:
			case ArchType.bpfeb:
				return "bpf";
			
			case ArchType.sparcv9:
			case ArchType.sparcel:
			case ArchType.sparc:
				return "sparc";
			
			case ArchType.systemz:
				return "s390";
			
			case ArchType.x86:
			case ArchType.x86_64:
				return "x86";
			
			case ArchType.xcore:
				return "xcore";
			
			case ArchType.nvptx:
			case ArchType.nvptx64:
				return "nvvm";
			
			case ArchType.le32:
				return "le32";
			
			case ArchType.le64:
				return "le64";
			
			case ArchType.amdil:
			case ArchType.amdil64:
				return "amdil";
			
			case ArchType.hsail:
			case ArchType.hsail64:
				return "hsail";
			
			case ArchType.spir:
			case ArchType.spir64:
				return "spir";
			
			case ArchType.spirv:
			case ArchType.spirv32:
			case ArchType.spirv64:
				return "spirv";
			
			case ArchType.kalimba:
				return "kalimba";
			
			case ArchType.lanai:
				return "lanai";
			
			case ArchType.shave:
				return "shave";
			
			case ArchType.wasm32:
			case ArchType.wasm64:
				return "wasm";
			
			case ArchType.riscv32:
			case ArchType.riscv64:
				return "riscv";
			
			case ArchType.ve:
				return "ve";
			
			case ArchType.csky:
				return "csky";
			
			case ArchType.loongarch32:
			case ArchType.loongarch64:
				return "loongarch";
			
			case ArchType.dxil:
				return "dx";
			
			case ArchType.xtensa:
				return "xtensa";
		}
	}

	public static string GetVendorTypeName(VendorType vendorType)
	{
		return vendorType switch
		{
			VendorType.UnknownVendor => "unknown",
			VendorType.AMD => "amd",
			VendorType.Apple => "apple",
			VendorType.CSR => "csr",
			VendorType.Freescale => "fsl",
			VendorType.IBM => "ibm",
			VendorType.ImaginationTechnologies => "img",
			VendorType.Mesa => "mesa",
			VendorType.MipsTechnologies => "mti",
			VendorType.NVIDIA => "nvidia",
			VendorType.OpenEmbedded => "oe",
			VendorType.PC => "pc",
			VendorType.SCEI => "scei",
			VendorType.SUSE => "suse",
			_ => throw new Exception("Invalid VendorType")
		};
	}

	public static string GetOSTypeName(OSType osType)
	{
		return osType switch
		{
			OSType.UnknownOS => "unknown",
			OSType.AIX => "aix",
			OSType.AMDHSA => "amdhsa",
			OSType.AMDPAL => "amdpal",
			OSType.CUDA => "cuda",
			OSType.Darwin => "darwin",
			OSType.DragonFly => "dragonfly",
			OSType.DriverKit => "driverkit",
			OSType.ELFIAMCU => "elfiamcu",
			OSType.Emscripten => "emscripten",
			OSType.FreeBSD => "freebsd",
			OSType.Fuchsia => "fuchsia",
			OSType.Haiku => "haiku",
			OSType.HermitCore => "hermit",
			OSType.Hurd => "hurd",
			OSType.IOS => "ios",
			OSType.KFreeBSD => "kfreebsd",
			OSType.Linux => "linux",
			OSType.Lv2 => "lv2",
			OSType.MacOSX => "macosx",
			OSType.Mesa3D => "mesa3d",
			OSType.NVCL => "nvcl",
			OSType.NaCl => "nacl",
			OSType.NetBSD => "netbsd",
			OSType.OpenBSD => "openbsd",
			OSType.PS4 => "ps4",
			OSType.PS5 => "ps5",
			OSType.RTEMS => "rtems",
			OSType.Solaris => "solaris",
			OSType.Serenity => "serenity",
			OSType.TvOS => "tvos",
			OSType.UEFI => "uefi",
			OSType.WASI => "wasi",
			OSType.WatchOS => "watchos",
			OSType.Win32 => "windows",
			OSType.ZOS => "zos",
			OSType.ShaderModel => "shadermodel",
			OSType.LiteOS => "liteos",
			_ => throw new Exception("Invalid OSType")
		};
	}

	public static string GetEnvironmentTypeName(EnvironmentType environmentType)
	{
		return environmentType switch
		{
			EnvironmentType.UnknownEnvironment => "unknown",
			EnvironmentType.Android => "android",
			EnvironmentType.CODE16 => "code16",
			EnvironmentType.CoreCLR => "coreclr",
			EnvironmentType.Cygnus => "cygnus",
			EnvironmentType.EABI => "eabi",
			EnvironmentType.EABIHF => "eabihf",
			EnvironmentType.GNU => "gnu",
			EnvironmentType.GNUABI64 => "gnuabi64",
			EnvironmentType.GNUABIN32 => "gnuabin32",
			EnvironmentType.GNUEABI => "gnueabi",
			EnvironmentType.GNUEABIHF => "gnueabihf",
			EnvironmentType.GNUF32 => "gnuf32",
			EnvironmentType.GNUF64 => "gnuf64",
			EnvironmentType.GNUSF => "gnusf",
			EnvironmentType.GNUX32 => "gnux32",
			EnvironmentType.GNUILP32 => "gnu_ilp32",
			EnvironmentType.Itanium => "itanium",
			EnvironmentType.MSVC => "msvc",
			EnvironmentType.MacABI => "macabi",
			EnvironmentType.Musl => "musl",
			EnvironmentType.MuslEABI => "musleabi",
			EnvironmentType.MuslEABIHF => "musleabihf",
			EnvironmentType.MuslX32 => "muslx32",
			EnvironmentType.Simulator => "simulator",
			EnvironmentType.Pixel => "pixel",
			EnvironmentType.Vertex => "vertex",
			EnvironmentType.Geometry => "geometry",
			EnvironmentType.Hull => "hull",
			EnvironmentType.Domain => "domain",
			EnvironmentType.Compute => "compute",
			EnvironmentType.Library => "library",
			EnvironmentType.RayGeneration => "raygeneration",
			EnvironmentType.Intersection => "intersection",
			EnvironmentType.AnyHit => "anyhit",
			EnvironmentType.ClosestHit => "closesthit",
			EnvironmentType.Miss => "miss",
			EnvironmentType.Callable => "callable",
			EnvironmentType.Mesh => "mesh",
			EnvironmentType.Amplification => "amplification",
			EnvironmentType.OpenHOS => "ohos",
			_ => throw new Exception("Invalid EnvironmentType")
		};
	}

	public static string GetObjectFormatTypeName(ObjectFormatType objectFormatType)
	{
		return objectFormatType switch
		{
			ObjectFormatType.UnknownObjectFormat => "",
			ObjectFormatType.COFF => "coff",
			ObjectFormatType.ELF => "elf",
			ObjectFormatType.GOFF => "goff",
			ObjectFormatType.MachO => "macho",
			ObjectFormatType.Wasm => "wasm",
			ObjectFormatType.XCOFF => "xcoff",
			ObjectFormatType.DXContainer => "dxcontainer",
			ObjectFormatType.SPIRV => "spirv",
			_ => throw new Exception("Invalid ObjectFormatType")
		};
	}

	private static unsafe bool IsHostLittleEndian()
	{
		var n = 1;
		return *(char*)&n == 1;
	}

	public static ArchType ParseBPFArch(string archName)
	{
		switch (archName)
		{
			case "bpf" when IsHostLittleEndian():
				return ArchType.bpfel;
			case "bpf":
			case "bpf_be" or "bpfeb":
				return ArchType.bpfeb;
			case "bpf_le" or "bpfel":
				return ArchType.bpfel;
			default:
				return ArchType.UnknownArch;
		}
	}

	public static ArchType GetArchTypeForLLVMName(string name)
	{
		return name switch
		{
			"aarch64" => ArchType.aarch64,
			"aarch64_be" => ArchType.aarch64_be,
			"aarch64_32" => ArchType.aarch64_32,
			"arc" => ArchType.arc,
			"arm64" => ArchType.aarch64,
			"arm64_32" => ArchType.aarch64_32,
			"arm" => ArchType.arm,
			"armeb" => ArchType.armeb,
			"avr" => ArchType.avr,
			"m68k" => ArchType.m68k,
			"mips" => ArchType.mips,
			"mipsel" => ArchType.mipsel,
			"mips64" => ArchType.mips64,
			"mips64el" => ArchType.mips64el,
			"msp430" => ArchType.msp430,
			"ppc64" => ArchType.ppc64,
			"ppc32" => ArchType.ppc,
			"ppc" => ArchType.ppc,
			"ppc32le" => ArchType.ppcle,
			"ppcle" => ArchType.ppcle,
			"ppc64le" => ArchType.ppc64le,
			"r600" => ArchType.r600,
			"amdgcn" => ArchType.amdgcn,
			"riscv32" => ArchType.riscv32,
			"riscv64" => ArchType.riscv64,
			"hexagon" => ArchType.hexagon,
			"sparc" => ArchType.sparc,
			"sparcel" => ArchType.sparcel,
			"sparcv9" => ArchType.sparcv9,
			"s390x" => ArchType.systemz,
			"systemz" => ArchType.systemz,
			"tce" => ArchType.tce,
			"tcele" => ArchType.tcele,
			"thumb" => ArchType.thumb,
			"thumbeb" => ArchType.thumbeb,
			"x86" => ArchType.x86,
			"i386" => ArchType.x86,
			"x86-64" => ArchType.x86_64,
			"xcore" => ArchType.xcore,
			"nvptx" => ArchType.nvptx,
			"nvptx64" => ArchType.nvptx64,
			"le32" => ArchType.le32,
			"le64" => ArchType.le64,
			"amdil" => ArchType.amdil,
			"amdil64" => ArchType.amdil64,
			"hsail" => ArchType.hsail,
			"hsail64" => ArchType.hsail64,
			"spir" => ArchType.spir,
			"spir64" => ArchType.spir64,
			"spirv" => ArchType.spirv,
			"spirv32" => ArchType.spirv32,
			"spirv64" => ArchType.spirv64,
			"kalimba" => ArchType.kalimba,
			"lanai" => ArchType.lanai,
			"shave" => ArchType.shave,
			"wasm32" => ArchType.wasm32,
			"wasm64" => ArchType.wasm64,
			"renderscript32" => ArchType.renderscript32,
			"renderscript64" => ArchType.renderscript64,
			"ve" => ArchType.ve,
			"csky" => ArchType.csky,
			"loongarch32" => ArchType.loongarch32,
			"loongarch64" => ArchType.loongarch64,
			"dxil" => ArchType.dxil,
			"xtensa" => ArchType.xtensa,
			_ => name.StartsWith("bpf") ? ParseBPFArch(name) : ArchType.UnknownArch
		};
	}

	public static ArchType ParseARMArch(string archName)
	{
		var arch = ArchType.UnknownArch;
		var isa = ARMTargetParser.ParseArchISA(archName);

		switch (ARMTargetParser.ParseArchEndian(archName))
		{
			case ARMTargetParser.EndianKind.Little:
				switch (isa)
				{
					case ARMTargetParser.ISAKind.ARM:
						arch = ArchType.arm;
						break;
					
					case ARMTargetParser.ISAKind.THUMB:
						arch = ArchType.thumb;
						break;
					
					case ARMTargetParser.ISAKind.AARCH64:
						arch = ArchType.aarch64;
						break;
					
					case ARMTargetParser.ISAKind.Invalid:
						break;
				}
				break;
			
			case ARMTargetParser.EndianKind.Big:
				switch (isa)
				{
					case ARMTargetParser.ISAKind.ARM:
						arch = ArchType.armeb;
						break;
					
					case ARMTargetParser.ISAKind.THUMB:
						arch = ArchType.thumbeb;
						break;
					
					case ARMTargetParser.ISAKind.AARCH64:
						arch = ArchType.aarch64_be;
						break;
					
					case ARMTargetParser.ISAKind.Invalid:
						break;
				}
				break;
			
			case ARMTargetParser.EndianKind.Invalid:
				break;
		}

		archName = ARMTargetParser.GetCanonicalArchName(archName);
		if (archName == "")
			return ArchType.UnknownArch;

		if (isa == ARMTargetParser.ISAKind.THUMB && (archName.StartsWith("v2") || archName.StartsWith("v3")))
			return ArchType.UnknownArch;
		
		// Todo
		return arch;
	}

	public readonly bool IsMacOSX() => OS is OSType.Darwin or OSType.MacOSX;
	public readonly bool IsiOS() => OS is OSType.IOS or OSType.TvOS;
	public readonly bool IsWatchABI() => SubArch == SubArchType.ARMSubArch_v7k;
	public readonly bool IsOSDarwin() => IsMacOSX() || IsiOS() || OS is OSType.WatchOS or OSType.DriverKit;
	public readonly bool IsSimulatorEnvironment() => Environment == EnvironmentType.Simulator;
	public readonly bool IsMacCatalystEnvironment() => Environment == EnvironmentType.MacABI;

	public readonly bool IsTargetMachineMac() =>
		IsMacOSX() || (IsOSDarwin() && (IsSimulatorEnvironment() || IsMacCatalystEnvironment()));

	public readonly bool IsPS4()
	{
		return Arch == ArchType.x86_64 && Vendor == VendorType.SCEI && OS == OSType.PS4;
	}

	public readonly bool IsPS5()
	{
		return Arch == ArchType.x86_64 && Vendor == VendorType.SCEI && OS == OSType.PS5;
	}

	public readonly bool IsPS() => IsPS4() || IsPS5();
	
	public readonly bool IsAndroid() => Environment == EnvironmentType.Android;

	public readonly bool IsShaderStageEnvironment()
	{
		return Environment is EnvironmentType.Pixel or EnvironmentType.Vertex or EnvironmentType.Geometry
			or EnvironmentType.Hull or EnvironmentType.Domain or EnvironmentType.Compute or EnvironmentType.Library
			or EnvironmentType.RayGeneration or EnvironmentType.Intersection or EnvironmentType.AnyHit
			or EnvironmentType.ClosestHit or EnvironmentType.Miss or EnvironmentType.Callable or EnvironmentType.Mesh
			or EnvironmentType.Amplification;
	}

	public readonly bool HasDLLImportExport() => OS == OSType.Win32 || IsPS();
	public readonly unsafe sbyte* CString() => CodeGenerator.ConvertString(data);
	public override string ToString() => data;
}