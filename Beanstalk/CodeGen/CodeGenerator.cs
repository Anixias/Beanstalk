using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Beanstalk.Analysis.Semantics;
using Beanstalk.Analysis.Syntax;
using LLVMSharp.Interop;
using ReferenceType = Beanstalk.Analysis.Semantics.ReferenceType;
using Type = Beanstalk.Analysis.Semantics.Type;

namespace Beanstalk.CodeGen;

internal readonly struct LLVMBool
{
	private readonly int value;

	private LLVMBool(int value)
	{
		this.value = value;
	}

	public static readonly LLVMBool False = new(0);
	public static readonly LLVMBool True = new(1);

	public static implicit operator int(LLVMBool value)
	{
		return value.value;
	}
}

public readonly unsafe struct OpaqueValue
{
	public readonly LLVMOpaqueValue* value;

	public static readonly LLVMOpaqueValue* NullPtr = (LLVMOpaqueValue*)nint.Zero;
	public static readonly OpaqueValue Null = new(NullPtr);

	public OpaqueValue(LLVMOpaqueValue* value)
	{
		this.value = value;
	}
}

internal readonly unsafe struct OpaqueType
{
	public readonly LLVMOpaqueType* value;
	public readonly List<FieldSymbol> fields = [];
	public readonly LLVMOpaqueValue* needsStaticInitialization;
	public readonly uint size;

	public static readonly LLVMOpaqueType* NullPtr = (LLVMOpaqueType*)nint.Zero;
	public static readonly OpaqueType Null = new(NullPtr, 0u);

	public OpaqueType(LLVMOpaqueType* value, uint size)
	{
		this.value = value;
		this.size = size;
		needsStaticInitialization = OpaqueValue.NullPtr;
	}

	public OpaqueType(LLVMOpaqueType* value, LLVMOpaqueValue* needsStaticInitialization, uint size)
	{
		this.value = value;
		this.needsStaticInitialization = needsStaticInitialization;
		this.size = size;
	}
}

internal unsafe class FunctionContext
{
	public readonly IFunctionSymbol functionSymbol;
	public readonly LLVMOpaqueType* functionType;
	public readonly LLVMOpaqueValue* functionValue;
	public readonly Dictionary<ParameterSymbol, OpaqueValue> parameterPointers = new();
	public readonly bool hasThisRef;

	public FunctionContext(IFunctionSymbol functionSymbol, LLVMOpaqueType* functionType, LLVMOpaqueValue* functionValue,
		bool hasThisRef)
	{
		this.functionSymbol = functionSymbol;
		this.functionType = functionType;
		this.functionValue = functionValue;
		this.hasThisRef = hasThisRef;
	}
}

internal unsafe class CallContext
{
	public readonly LLVMOpaqueValue* thisPtr;

	public CallContext(LLVMOpaqueValue* thisPtr)
	{
		this.thisPtr = thisPtr;
	}
}

public unsafe partial class CodeGenerator : ResolvedStatementNode.IVisitor, ResolvedExpressionNode.IVisitor<OpaqueValue>
{
	private enum CodeGenerationPass
	{
		TopLevelDeclarations,
		MemberDeclarations,
		MethodDeclarations,
		Definitions,
		Complete
	}
	
	public bool Debug { get; set; }
	
	private LLVMOpaqueModule* currentModule;
	private LLVMOpaqueContext* currentContext;
	private LLVMOpaqueBuilder* currentBuilder;
	private Target? currentTarget;
	private CodeGenerationPass currentPass;
	private readonly Dictionary<string, OpaqueValue> constantLiterals = new();
	private readonly Dictionary<ISymbol, OpaqueValue> valueSymbols = new(); 
	private readonly Dictionary<ISymbol, OpaqueType> typeSymbols = new();
	private readonly Dictionary<IFunctionSymbol, FunctionContext> functionContexts = new();
	private readonly Stack<FunctionContext> functionStack = new();
	private FunctionContext CurrentFunctionContext => functionStack.Peek();
	private readonly Stack<CallContext> callStack = new();
	private CallContext CurrentCallContext => callStack.Peek();
	private readonly Stack<bool> lvalueStack = new();
	private bool CurrentIsLValue => lvalueStack.TryPeek(out var isLValue) && isLValue;
	private readonly Stack<OpaqueValue> thisStack = new();
	private OpaqueValue CurrentThisValue => thisStack.Peek();

	private static readonly sbyte* EmptyString = ConvertString("");

	private static string ExtractResource(string resource)
	{
		var path = Path.Combine(Path.GetTempPath(), resource);
		var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Beanstalk.Resources.{resource}");

		if (stream is null)
			throw new Exception($"Unable to extract resource '{resource}'");
		
		var bytes = new byte[(int)stream.Length];
		if (stream.Read(bytes, 0, bytes.Length) != bytes.Length)
			throw new Exception($"Unable to extract resource '{resource}'");
		
		File.WriteAllBytes(path, bytes);
		return path;
	}
	
	public string Generate(IEnumerable<ResolvedAst> asts, Target? target, int optimizationLevel, string outputPath)
	{
		//LLVM.InitializeAllTargets();
		currentTarget = target;
		var passManager = LLVM.CreatePassManager();
		
		// Todo: Handle optimization levels correctly
		//LLVM.AddInstructionCombiningPass(passManager);
		//LLVM.AddInstructionSimplifyPass(passManager);
		//LLVM.AddMemCpyOptPass(passManager);
		
		var objDirectory = Path.GetDirectoryName(outputPath) + "/obj/";
		Directory.CreateDirectory(objDirectory);
		foreach (var file in Directory.EnumerateFiles(objDirectory, "*", SearchOption.AllDirectories))
		{
			File.Delete(file);
		}
		
		var binDirectory = Path.GetDirectoryName(outputPath) + "/bin/";
		Directory.CreateDirectory(binDirectory);
		outputPath = Path.Combine(binDirectory, Path.GetFileName(outputPath));
		
		var sourceFiles = new List<string>();
		foreach (var ast in asts)
		{
			var relativePath = Path.GetRelativePath(ast.WorkingDirectory, ast.FilePath);
			currentModule = LLVM.ModuleCreateWithName(ConvertString(relativePath));
			currentContext = LLVM.GetModuleContext(currentModule);
			currentBuilder = LLVM.CreateBuilderInContext(currentContext);
			currentPass = CodeGenerationPass.TopLevelDeclarations;
			constantLiterals.Clear();
			valueSymbols.Clear();
			typeSymbols.Clear();
			functionContexts.Clear();
			functionStack.Clear();
			
			if (target is not null)
				LLVM.SetTarget(currentModule, target.triple.CString());

			while (currentPass < CodeGenerationPass.Complete)
			{
				switch (ast.Root)
				{
					case ResolvedStatementNode statementNode:
						statementNode.Accept(this);
						break;
				}

				currentPass++;
			}

			// Todo: Handle errors?
			LLVM.RunPassManager(passManager, currentModule);

			if (Debug)
				LLVM.DumpModule(currentModule);

			var outputBitCodePath = Path.Combine(objDirectory, Path.ChangeExtension(relativePath, ".bc"));
			Directory.CreateDirectory(Path.GetDirectoryName(outputBitCodePath)!);
			if (LLVM.WriteBitcodeToFile(currentModule, ConvertString(outputBitCodePath)) != LLVM.LLVMErrorSuccess)
				throw new InvalidOperationException($"Failed to emit IR for source file: {relativePath}");
			
			sourceFiles.Add(outputBitCodePath);
		}

		currentTarget = null;
		
		var clang = ExtractResource("clang.exe");
		var llvmAr = ExtractResource("llvm-ar.exe");
		var beanstalkLib = ExtractResource("beanstalk.lib");

		var beanstalkLibDirectory = Path.GetDirectoryName(beanstalkLib);
		var beanstalkLibFileName = Path.GetFileName(beanstalkLib);
		var beanstalkLibLinkArgs = $"--library-directory={beanstalkLibDirectory} -l{beanstalkLibFileName}";

		var targetArg = target is null ? "" : $"-target {target.triple}";
		
		var extension = Path.GetExtension(outputPath);
		try
		{
			Process process;
			switch (extension)
			{
				case ".dll":
				case ".so":
				{
					var objectFiles = new List<string>();
					foreach (var file in sourceFiles)
					{
						var objectFile = Path.ChangeExtension(file, ".o");
						objectFiles.Add(objectFile);

						var processStartInfo = new ProcessStartInfo
						{
							FileName = clang,
							Arguments = $"{file} {targetArg} --compile --output={objectFile}",
							WindowStyle = ProcessWindowStyle.Hidden
						};

						process = new Process
						{
							StartInfo = processStartInfo
						};

						process.Start();
						process.WaitForExit();
					}

					var clangStartInfo = new ProcessStartInfo
					{
						FileName = clang,
						Arguments = $"{string.Join(' ', objectFiles)} {beanstalkLibLinkArgs}  --shared -fPIC " +
						            $"--output={outputPath} {targetArg}",
						WindowStyle = ProcessWindowStyle.Hidden
					};

					process = new Process
					{
						StartInfo = clangStartInfo
					};

					process.Start();
					process.WaitForExit();
				}
					break;

				case ".lib":
				case ".a":
				{
					var objectFiles = new List<string>();
					foreach (var file in sourceFiles)
					{
						var objectFile = Path.ChangeExtension(file, ".o");
						objectFiles.Add(objectFile);

						var processStartInfo = new ProcessStartInfo
						{
							FileName = clang,
							Arguments = $"{file} {targetArg} --compile --output={objectFile}",
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
						FileName = llvmAr,
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
				case ".bin":
				case "":
				{
					var processStartInfo = new ProcessStartInfo
					{
						FileName = clang,
						Arguments = $"{string.Join(' ', sourceFiles)} {targetArg} --output={outputPath} " +
						            $"{beanstalkLibLinkArgs}",
						WindowStyle = ProcessWindowStyle.Hidden,
						UseShellExecute = false
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

			return outputPath;
		}
		finally
		{
			File.Delete(clang);
			File.Delete(llvmAr);
			File.Delete(beanstalkLib);
		}
	}

	internal static sbyte* ConvertString(string text)
	{
		var bytes = Encoding.ASCII.GetBytes($"{text}\0");
		fixed (byte* p = bytes)
		{
			return (sbyte*)p;
		}
	}

	private static sbyte* ConvertUnicodeString(string text, out uint length)
	{
		var bytes = Encoding.UTF8.GetBytes($"{text}\0");
		length = (uint)bytes.LongLength;
		fixed (byte* p = bytes)
		{
			return (sbyte*)p;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T** ConvertArrayToPointer<T>(T*[] values) where T : unmanaged
	{
		fixed (T** p = values)
		{
			return p;
		}
	}

	private LLVMOpaqueType* GetType(Type? type)
	{
		switch (type)
		{
			default:
				throw new NotImplementedException();
			
			case null:
				return LLVM.VoidType();
			
			case NullableType nullableType:
			{
				var baseType = GetType(nullableType.baseType);
				return LLVM.PointerType(baseType, 0u);
			}
			
			case ReferenceType referenceType:
			{
				var baseType = GetType(referenceType.baseType);
				if (referenceType.baseType is NullableType)
					return baseType;
				
				return LLVM.PointerType(baseType, 0u);
			}
			
			case BaseType baseType:
			{
				return baseType.typeSymbol switch
				{
					NativeSymbol nativeSymbol => GetNativeType(nativeSymbol),
					_ => typeSymbols[baseType.typeSymbol].value
				};
			}
		}
	}

	private uint GetSize(Type type)
	{
		var ptrSize = currentTarget?.PointerSize() ?? (uint)sizeof(nint);
		switch (type)
		{
			default:
				throw new NotImplementedException();
			
			case NullableType nullableType:
			{
				return ptrSize;
			}
			
			case ReferenceType referenceType:
			{
				return ptrSize;
			}
			
			case BaseType baseType:
			{
				return baseType.typeSymbol switch
				{
					NativeSymbol nativeSymbol => GetNativeSize(nativeSymbol),
					_ => typeSymbols[baseType.typeSymbol].size
				};
			}
		}
	}

	private LLVMOpaqueType* GetNativeType(NativeSymbol nativeSymbol)
	{
		if (nativeSymbol == TypeSymbol.Int8)
			return LLVM.Int8TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Int16)
			return LLVM.Int16TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Int32)
			return LLVM.Int32TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Int64)
			return LLVM.Int64TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Int128)
			return LLVM.Int128TypeInContext(currentContext);
		
		// LLVM Does not distinguish between signed and unsigned types except for via instructions generated
		if (nativeSymbol == TypeSymbol.UInt8)
			return LLVM.Int8TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.UInt16)
			return LLVM.Int16TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.UInt32)
			return LLVM.Int32TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.UInt64)
			return LLVM.Int64TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.UInt128)
			return LLVM.Int128TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Float32)
			return LLVM.FloatTypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Float64)
			return LLVM.DoubleTypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Float128)
			return LLVM.FP128TypeInContext(currentContext);
		
		// Todo: Handle fixed point types
		
		if (nativeSymbol == TypeSymbol.Bool)
			return LLVM.Int1TypeInContext(currentContext);
		
		if (nativeSymbol == TypeSymbol.Char)
			return LLVM.ArrayType(LLVM.Int8TypeInContext(currentContext), 4u);
		
		if (nativeSymbol == TypeSymbol.String)
			return LLVM.PointerType(LLVM.Int8TypeInContext(currentContext), 0u);

		return LLVM.VoidTypeInContext(currentContext);
	}

	private uint GetNativeSize(NativeSymbol nativeSymbol)
	{
		if (nativeSymbol == TypeSymbol.Int8)
			return 1u;

		if (nativeSymbol == TypeSymbol.Int16)
			return 2u;
		
		if (nativeSymbol == TypeSymbol.Int32)
			return 4u;
		
		if (nativeSymbol == TypeSymbol.Int64)
			return 8u;
		
		if (nativeSymbol == TypeSymbol.Int128)
			return 16u;
		
		// LLVM Does not distinguish between signed and unsigned types except for via instructions generated
		if (nativeSymbol == TypeSymbol.UInt8)
			return 1u;
		
		if (nativeSymbol == TypeSymbol.UInt16)
			return 2u;
		
		if (nativeSymbol == TypeSymbol.UInt32)
			return 4u;
		
		if (nativeSymbol == TypeSymbol.UInt64)
			return 8u;
		
		if (nativeSymbol == TypeSymbol.UInt128)
			return 16u;
		
		if (nativeSymbol == TypeSymbol.Float32)
			return 4u;
		
		if (nativeSymbol == TypeSymbol.Float64)
			return 8u;
		
		if (nativeSymbol == TypeSymbol.Float128)
			return 16u;
		
		// Todo: Handle fixed point types
		
		if (nativeSymbol == TypeSymbol.Bool)
			return 1u;
		
		if (nativeSymbol == TypeSymbol.Char)
			return 4u;
		
		// Todo: Verify this works as expected
		if (nativeSymbol == TypeSymbol.String)
			return 1u;

		throw new InvalidOperationException("Failed to get size of type: Unsupported native type");
	}

	public void Visit(ResolvedProgramStatement programStatement)
	{
		if (currentPass == CodeGenerationPass.TopLevelDeclarations)
			ImportSymbols(programStatement.importedSymbols);
		
		foreach (var statement in programStatement.topLevelStatements)
		{
			statement.Accept(this);
		}
	}

	public void Visit(ResolvedModuleStatement moduleStatement)
	{
		foreach (var statement in moduleStatement.topLevelStatements)
		{
			statement.Accept(this);
		}
	}

	public void Visit(ResolvedStructDeclarationStatement structDeclarationStatement)
	{
		var structSymbol = structDeclarationStatement.structSymbol;

		switch (currentPass)
		{
			case CodeGenerationPass.TopLevelDeclarations:
			{
				DeclareStruct(structSymbol);
				break;
			}

			case CodeGenerationPass.MemberDeclarations:
			{
				if (!typeSymbols.ContainsKey(structSymbol))
					throw new Exception($"Struct '{structSymbol.Name}' " +
					                    $"not forward declared");

				var structOpaqueType = typeSymbols[structSymbol];
				var structType = structOpaqueType.value;

				var totalBytes = 0u;
				var elementTypeList = new List<Type>();
				foreach (var statement in structDeclarationStatement.statements)
				{
					switch (statement)
					{
						case ResolvedFieldDeclarationStatement fieldDeclarationStatement:
						{
							if (fieldDeclarationStatement.fieldSymbol.IsStatic)
								break;

							elementTypeList.Add(fieldDeclarationStatement.fieldSymbol.EvaluatedType!);
							structOpaqueType.fields.Add(fieldDeclarationStatement.fieldSymbol);
							break;
						}
					}
				}

				var elementTypes = new LLVMOpaqueType*[elementTypeList.Count];
				for (var i = 0; i < elementTypes.Length; i++)
				{
					elementTypes[i] = GetType(elementTypeList[i]);
					totalBytes += GetSize(elementTypeList[i]);
				}

				if (totalBytes == 0u)
				{
					// Todo: Handle empty structs
				}

				LLVM.StructSetBody(structType, ConvertArrayToPointer(elementTypes), (uint)elementTypes.LongLength,
					LLVMBool.False);

				typeSymbols[structSymbol] = new OpaqueType(structOpaqueType.value,
					structOpaqueType.needsStaticInitialization, totalBytes);

				break;
			}

			case CodeGenerationPass.MethodDeclarations:
			{
				if (!typeSymbols.ContainsKey(structSymbol))
					throw new Exception($"Struct '{structSymbol.Name}' " +
					                    $"not forward declared");

				var structOpaqueType = typeSymbols[structSymbol];
				foreach (var statement in structDeclarationStatement.statements)
				{
					switch (statement)
					{
						case ResolvedConstructorDeclarationStatement constructorDeclarationStatement:
						{
							var constructorSymbol = constructorDeclarationStatement.constructorSymbol;
							
							var parameterNameList = new List<string>
							{
								new(constructorSymbol.This.Name)
							};
							
							var parameterTypeList = new List<OpaqueType>
							{
								new(GetType(constructorSymbol.This.EvaluatedType),
									GetSize(constructorSymbol.This.EvaluatedType!))
							};

							foreach (var parameter in constructorSymbol.Parameters)
							{
								var paramType = GetType(parameter.EvaluatedType);
								var paramSize = GetSize(parameter.EvaluatedType!);
								parameterNameList.Add(parameter.Name);
								parameterTypeList.Add(new OpaqueType(paramType, paramSize));
							}

							var parameterTypes = new LLVMOpaqueType*[parameterTypeList.Count];
							for (var i = 0; i < parameterTypes.Length; i++)
							{
								parameterTypes[i] = parameterTypeList[i].value;
							}

							var constructorType = LLVM.FunctionType(LLVM.VoidTypeInContext(currentContext),
								ConvertArrayToPointer(parameterTypes), (uint)parameterTypes.LongLength, LLVMBool.False);

							var constructor = LLVM.AddFunction(currentModule,
								ConvertString($"{structSymbol.Name}.new"), constructorType);

							if (Debug)
							{
								for (var i = 0; i < parameterTypes.Length; i++)
								{
									var param = LLVM.GetParam(constructor, (uint)i);
									var name = parameterNameList[i];
									var nameLength = (nuint)name.Length;
									LLVM.SetValueName2(param, ConvertString(name), nameLength);
								}
							}

							valueSymbols.Add(constructorSymbol, new OpaqueValue(constructor));
							var context = new FunctionContext(constructorSymbol, constructorType, constructor, true);
							functionContexts.Add(constructorSymbol, context);
							break;
						}
						
						case ResolvedStringDeclarationStatement stringDeclarationStatement:
						{
							var stringFunctionSymbol = stringDeclarationStatement.stringFunctionSymbol;
							
							var parameterNameList = new List<string>
							{
								new(stringFunctionSymbol.This.Name)
							};
							
							var parameterTypeList = new List<OpaqueType>
							{
								new(GetType(stringFunctionSymbol.This.EvaluatedType),
									GetSize(stringFunctionSymbol.This.EvaluatedType!))
							};

							var parameterTypes = new LLVMOpaqueType*[parameterTypeList.Count];
							for (var i = 0; i < parameterTypes.Length; i++)
							{
								parameterTypes[i] = parameterTypeList[i].value;
							}

							var functionType = LLVM.FunctionType(GetNativeType(TypeSymbol.String),
								ConvertArrayToPointer(parameterTypes), (uint)parameterTypes.LongLength, LLVMBool.False);

							var constructor = LLVM.AddFunction(currentModule,
								ConvertString($"{structSymbol.Name}.string"), functionType);

							if (Debug)
							{
								for (var i = 0; i < parameterTypes.Length; i++)
								{
									var param = LLVM.GetParam(constructor, (uint)i);
									var name = parameterNameList[i];
									var nameLength = (nuint)name.Length;
									LLVM.SetValueName2(param, ConvertString(name), nameLength);
								}
							}

							valueSymbols.Add(stringFunctionSymbol, new OpaqueValue(constructor));
							var context = new FunctionContext(stringFunctionSymbol, functionType, constructor, true);
							functionContexts.Add(stringFunctionSymbol, context);
							break;
						}
						
						case ResolvedOperatorDeclarationStatement operatorDeclarationStatement:
						{
							if (operatorDeclarationStatement.operatorOverloadSymbol.IsNative)
								break;
							
							switch (operatorDeclarationStatement.operatorOverloadSymbol)
							{
								case BinaryOperatorOverloadSymbol operatorOverloadSymbol:
								{
									var parameterNameList = new string[]
									{
										new(operatorOverloadSymbol.Left.Name),
										new(operatorOverloadSymbol.Right.Name)
									};
									
									var parameterTypeList = new OpaqueType[]
									{
										new(GetType(operatorOverloadSymbol.Left.EvaluatedType), 
											GetSize(operatorOverloadSymbol.Left.EvaluatedType!)),
										
										new(GetType(operatorOverloadSymbol.Right.EvaluatedType), 
											GetSize(operatorOverloadSymbol.Right.EvaluatedType!))
									};

									var parameterTypes = new LLVMOpaqueType*[parameterTypeList.Length];
									for (var i = 0; i < parameterTypes.Length; i++)
									{
										parameterTypes[i] = parameterTypeList[i].value;
									}

									var returnType = GetType(operatorOverloadSymbol.ReturnType);
									var functionType = LLVM.FunctionType(returnType,
										ConvertArrayToPointer(parameterTypes), (uint)parameterTypes.LongLength,
										LLVMBool.False);

									var function = LLVM.AddFunction(currentModule,
										ConvertString(operatorOverloadSymbol.Name[1..]), functionType);

									if (Debug)
									{
										for (var i = 0u; i < parameterTypes.Length; i++)
										{
											var param = LLVM.GetParam(function, i);
											var name = parameterNameList[i];
											var nameLength = (nuint)name.Length;
											LLVM.SetValueName2(param, ConvertString(name), nameLength);
										}
									}

									valueSymbols.Add(operatorOverloadSymbol, new OpaqueValue(function));
									var context = new FunctionContext(operatorOverloadSymbol, functionType, function,
										false);
									
									functionContexts.Add(operatorOverloadSymbol, context);
									break;
								}
								
								case UnaryOperatorOverloadSymbol operatorOverloadSymbol:
								{
									var parameterNameList = new string[]
									{
										new(operatorOverloadSymbol.Operand.Name)
									};
									
									var parameterTypeList = new OpaqueType[]
									{
										new(GetType(operatorOverloadSymbol.Operand.EvaluatedType),
											GetSize(operatorOverloadSymbol.Operand.EvaluatedType))
									};

									var parameterTypes = new LLVMOpaqueType*[parameterTypeList.Length];
									for (var i = 0; i < parameterTypes.Length; i++)
									{
										parameterTypes[i] = parameterTypeList[i].value;
									}

									var returnType = GetType(operatorOverloadSymbol.ReturnType);
									var functionType = LLVM.FunctionType(returnType,
										ConvertArrayToPointer(parameterTypes), (uint)parameterTypes.LongLength,
										LLVMBool.False);

									var function = LLVM.AddFunction(currentModule,
										ConvertString(operatorOverloadSymbol.Name[1..]), functionType);

									if (Debug)
									{
										for (var i = 0u; i < parameterTypes.Length; i++)
										{
											var param = LLVM.GetParam(function, i);
											var name = parameterNameList[i];
											var nameLength = (nuint)name.Length;
											LLVM.SetValueName2(param, ConvertString(name), nameLength);
										}
									}

									valueSymbols.Add(operatorOverloadSymbol, new OpaqueValue(function));
									var context = new FunctionContext(operatorOverloadSymbol, functionType, function,
										false);
									functionContexts.Add(operatorOverloadSymbol, context);
									break;
								}
							}

							break;
						}
					}
				}

				typeSymbols[structSymbol] = new OpaqueType(structOpaqueType.value,
					structOpaqueType.needsStaticInitialization, structOpaqueType.size);

				break;
			}

			case CodeGenerationPass.Definitions:
			{
				foreach (var statement in structDeclarationStatement.statements)
				{
					statement.Accept(this);
				}
				
				break;
			}
		}
	}

	public void Visit(ResolvedFieldDeclarationStatement statement)
	{
		// Do nothing
	}

	public void Visit(ResolvedConstDeclarationStatement statement)
	{
		// Do nothing
	}

	public void Visit(ResolvedEntryStatement entryStatement)
	{
		if (currentPass != CodeGenerationPass.Definitions)
			return;
		
		// Creation
		var entryType = LLVM.FunctionType(LLVM.Int32Type(), (LLVMOpaqueType**)nint.Zero, 0u, LLVMBool.False);
		var entryPoint = LLVM.AddFunction(currentModule, ConvertString("main"), entryType);
		
		// Positioning
		var entryBody = LLVM.AppendBasicBlockInContext(currentContext, entryPoint, EmptyString);
		LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		
		// Instructions
		var context = new FunctionContext(entryStatement.entrySymbol!, entryType, entryPoint, false);
		functionContexts.Add(entryStatement.entrySymbol!, context);
		functionStack.Push(context);
		foreach (var statement in entryStatement.statements)
		{
			statement.Accept(this);
			//LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		}
		functionStack.Pop();
		
		// Verification
		if (LLVM.VerifyFunction(entryPoint, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(entryPoint);
	}

	public void Visit(ResolvedExternalFunctionStatement statement)
	{
		if (currentPass != CodeGenerationPass.TopLevelDeclarations)
			return;

		DeclareExternalFunction(statement.externalFunctionSymbol);
	}

	public void Visit(ResolvedFunctionDeclarationStatement statement)
	{
		// Todo
		throw new NotImplementedException();
	}

	public void Visit(ResolvedConstructorDeclarationStatement statement)
	{
		if (currentPass != CodeGenerationPass.Definitions)
			return;
		
		var constructorSymbol = statement.constructorSymbol;

		if (!valueSymbols.ContainsKey(constructorSymbol))
			throw new InvalidOperationException($"Constructor for type '{constructorSymbol.Owner.Name}' " +
			                                    "not forward declared");
		
		var constructor = valueSymbols[constructorSymbol].value;
		
		if (constructor == OpaqueValue.NullPtr || LLVM.IsUndef(constructor) == LLVMBool.True)
			throw new InvalidOperationException(
				$"Unable to resolve constructor for type '{constructorSymbol.Owner.Name}'");
		
		// Body
		if (!functionContexts.TryGetValue(constructorSymbol, out var context))
			throw new InvalidOperationException("Constructor missing function context");
		
		// Positioning
		var entryBody = LLVM.AppendBasicBlockInContext(currentContext, constructor, EmptyString);
		LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		
		// Instructions
		if (!typeSymbols.TryGetValue(constructorSymbol.Owner, out var ownerType))
			throw new InvalidOperationException($"Unable to resolve type '{constructorSymbol.Owner.Name}'");

		var thisParam = LLVM.GetFirstParam(constructor);
		if (thisParam == OpaqueValue.NullPtr || LLVM.IsUndef(thisParam) == LLVMBool.True)
			throw new InvalidOperationException("Unable to retrieve implicit 'this' parameter");
		
		functionStack.Push(context);
		
		// Default initializers
		foreach (var field in constructorSymbol.Owner.SymbolTable.Values.OfType<FieldSymbol>())
		{
			if (field.IsStatic)
				continue;

			if (field.Initializer is not { } initializer)
				continue;

			var elementPtr = BuildStructGEP(ownerType.value, thisParam, field.Index, field.Name);

			var initializerValue = initializer.Accept(this);
			LLVM.BuildStore(currentBuilder, initializerValue.value, elementPtr);
		}
		
		// Parameters
		foreach (var parameter in constructorSymbol.Parameters)
		{
			var param = LLVM.GetParam(constructor, parameter.Index + 1u);
			var pointerType = LLVM.PointerType(GetType(parameter.EvaluatedType), 0u);
			var parameterSize = GetSize(parameter.EvaluatedType!);
			var pointer = BuildAlloca(pointerType, $"{parameter.Name}.addr", parameterSize);
			BuildStore(param, pointer, parameterSize);
			CurrentFunctionContext.parameterPointers.Add(parameter, new OpaqueValue(pointer));
		}
		
		// Body statements
		statement.body.Accept(this);
		LLVM.BuildRetVoid(currentBuilder);
		functionStack.Pop();
	
		// Verification
		if (LLVM.VerifyFunction(constructor, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(constructor);
	}

	public void Visit(ResolvedDestructorDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ResolvedStringDeclarationStatement statement)
	{
		if (currentPass != CodeGenerationPass.Definitions)
			return;
		
		var stringFunctionSymbol = statement.stringFunctionSymbol;

		if (!valueSymbols.ContainsKey(stringFunctionSymbol))
			throw new InvalidOperationException("String function not forward declared");
		
		var stringFunction = valueSymbols[stringFunctionSymbol].value;
		
		if (stringFunction == OpaqueValue.NullPtr || LLVM.IsUndef(stringFunction) == LLVMBool.True)
			throw new InvalidOperationException("Unable to resolve string function");
		
		// Body
		if (!functionContexts.TryGetValue(stringFunctionSymbol, out var context))
			throw new InvalidOperationException("String function missing function context");
		
		// Positioning
		var entryBody = LLVM.AppendBasicBlockInContext(currentContext, stringFunction, EmptyString);
		LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		
		// Instructions
		functionStack.Push(context);
		statement.body.Accept(this);
		functionStack.Pop();
	
		// Verification
		if (LLVM.VerifyFunction(stringFunction, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(stringFunction);
	}

	public void Visit(ResolvedOperatorDeclarationStatement statement)
	{
		if (currentPass != CodeGenerationPass.Definitions)
			return;
		
		var operatorOverloadSymbol = statement.operatorOverloadSymbol;

		if (!valueSymbols.ContainsKey(operatorOverloadSymbol))
			throw new InvalidOperationException("Operator overload not forward declared");
		
		var operatorOverload = valueSymbols[operatorOverloadSymbol].value;
		
		if (operatorOverload == OpaqueValue.NullPtr || LLVM.IsUndef(operatorOverload) == LLVMBool.True)
			throw new InvalidOperationException("Unable to resolve operator overload");
		
		// Body
		if (!functionContexts.TryGetValue(operatorOverloadSymbol, out var context))
			throw new InvalidOperationException("Operator overload missing function context");
		
		// Positioning
		var entryBody = LLVM.AppendBasicBlockInContext(currentContext, operatorOverload, EmptyString);
		LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		
		// Parameters
		functionStack.Push(context);
		switch (operatorOverloadSymbol)
		{
			case BinaryOperatorOverloadSymbol symbol:
			{
				var leftParam = LLVM.GetParam(operatorOverload, 0u);
				var leftPtrType = LLVM.PointerType(GetType(symbol.Left.EvaluatedType), 0u);
				var leftSize = GetSize(symbol.Left.EvaluatedType!);
				var leftPtr = BuildAlloca(leftPtrType, $"{symbol.Left.Name}.addr", leftSize);
				BuildStore(leftParam, leftPtr, leftSize);
				CurrentFunctionContext.parameterPointers.Add(symbol.Left, new OpaqueValue(leftPtr));
				
				var rightParam = LLVM.GetParam(operatorOverload, 1u);
				var rightPtrType = LLVM.PointerType(GetType(symbol.Right.EvaluatedType), 0u);
				var rightSize = GetSize(symbol.Right.EvaluatedType!);
				var rightPtr = BuildAlloca(rightPtrType, $"{symbol.Right.Name}.addr", rightSize);
				BuildStore(rightParam, rightPtr, rightSize);
				CurrentFunctionContext.parameterPointers.Add(symbol.Right, new OpaqueValue(rightPtr));
				
				break;
			}

			case UnaryOperatorOverloadSymbol symbol:
			{
				var param = LLVM.GetParam(operatorOverload, 0u);
				var pointerType = LLVM.PointerType(GetType(symbol.Operand.EvaluatedType), 0u);
				var operandSize = GetSize(symbol.Operand.EvaluatedType!);
				var pointer = BuildAlloca(pointerType, $"{symbol.Operand.Name}.addr", operandSize);
				BuildStore(param, pointer, operandSize);
				CurrentFunctionContext.parameterPointers.Add(symbol.Operand, new OpaqueValue(pointer));
				
				break;
			}
		}
		
		// Instructions
		statement.body.Accept(this);
		functionStack.Pop();
	
		// Verification
		if (LLVM.VerifyFunction(operatorOverload, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(operatorOverload);
	}

	public void Visit(ResolvedExpressionStatement statement)
	{
		statement.value.Accept(this);
	}

	public void Visit(ResolvedReturnStatement statement)
	{
		if (statement.value?.Accept(this) is { } value)
			LLVM.BuildRet(currentBuilder, value.value);
		else
			LLVM.BuildRetVoid(currentBuilder);
	}

	public void Visit(ResolvedBlockStatement statement)
	{
		foreach (var bodyStatement in statement.statements)
		{
			bodyStatement.Accept(this);
		}
	}

	public void Visit(ResolvedVarDeclarationStatement statement)
	{
		var type = GetType(statement.varSymbol.EvaluatedType);
		if (type == LLVM.VoidTypeInContext(currentContext))
			throw new InvalidOperationException($"Type of variable '{statement.varSymbol.Name}' cannot be inferred");
		
		var size = GetSize(statement.varSymbol.EvaluatedType!);
		var allocation = BuildAlloca(type, statement.varSymbol.Name, size);

		valueSymbols.Add(statement.varSymbol, new OpaqueValue(allocation));

		if (statement.initializer?.Accept(this) is { } initializer)
		{
			BuildStore(initializer.value, allocation, size);
		}
	}

	public void Visit(ResolvedSimpleStatement statement)
	{
		throw new InvalidOperationException();
	}

	public OpaqueValue Visit(ResolvedFunctionSymbolExpression symbolExpression)
	{
		throw new InvalidOperationException();
	}

	public OpaqueValue Visit(ResolvedConstructorSymbolExpression symbolExpression)
	{
		throw new InvalidOperationException();
	}

	public OpaqueValue Visit(ResolvedStringFunctionSymbolExpression symbolExpression)
	{
		throw new InvalidOperationException();
	}

	public OpaqueValue Visit(ResolvedExternalFunctionSymbolExpression symbolExpression)
	{
		throw new InvalidOperationException();
	}

	public OpaqueValue Visit(ResolvedFunctionCallExpression expression)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedConstructorCallExpression expression)
	{
		var constructorSymbol = expression.constructorSymbol;
		var ownerSymbol = constructorSymbol.Owner;

		if (!valueSymbols.ContainsKey(constructorSymbol))
			throw new Exception($"Constructor for type '{constructorSymbol.Owner.Name}' not forward declared");

		if (!typeSymbols.ContainsKey(ownerSymbol))
			throw new Exception($"Type '{ownerSymbol.Name}' not forward declared");
		
		var ownerValue = typeSymbols[ownerSymbol];
		var owner = ownerValue.value;
		var constructor = valueSymbols[constructorSymbol].value;
		
		if (constructor == OpaqueValue.NullPtr || LLVM.IsUndef(constructor) == LLVMBool.True)
			throw new InvalidOperationException(
				$"Unable to resolve constructor for type '{constructorSymbol.Owner.Name}'");
		
		// Allocate memory
		// Todo: Memory management!
		var thisAllocation = BuildAlloca(owner, $"{ownerSymbol.Name}.new", ownerValue.size);

		var arguments = new LLVMOpaqueValue*[expression.arguments.Length + 1];
		for (var i = 1; i < arguments.Length; i++)
		{
			var arg = expression.arguments[i - 1];
			arguments[i] = arg.Accept(this).value;
		}

		arguments[0] = thisAllocation;
		BuildCall(constructor, arguments, "");

		if (!CurrentIsLValue)
		{
			return new OpaqueValue(BuildLoad(owner, thisAllocation, "", ownerValue.size));
		}

		return new OpaqueValue(thisAllocation);
	}

	public OpaqueValue Visit(ResolvedStringCallExpression expression)
	{
		var functionSymbol = expression.stringFunctionSymbol;
		var ownerSymbol = functionSymbol.Owner;

		if (!valueSymbols.ContainsKey(functionSymbol))
			throw new Exception($"String function for type '{functionSymbol.Owner.Name}' not forward declared");

		if (!typeSymbols.ContainsKey(ownerSymbol))
			throw new Exception($"Type '{ownerSymbol.Name}' not forward declared");
		
		var ownerValue = typeSymbols[ownerSymbol];
		var owner = ownerValue.value;

		if (!valueSymbols.ContainsKey(functionSymbol))
			throw new InvalidOperationException("String function not forward declared");
		
		var function = valueSymbols[functionSymbol].value;
		
		if (function == OpaqueValue.NullPtr || LLVM.IsUndef(function) == LLVMBool.True)
			throw new InvalidOperationException("Unable to resolve string function");
		
		lvalueStack.Push(true);
		var source = expression.source.Accept(this).value;
		lvalueStack.Pop();
		
		var arguments = new LLVMOpaqueValue*[] { source };

		return new OpaqueValue(BuildCall(function, arguments, ""));
	}

	public OpaqueValue Visit(ResolvedExternalFunctionCallExpression expression)
	{
		var functionSymbol = expression.functionSymbol;
		var functionName = functionSymbol.Attributes.GetValueOrDefault("entry", functionSymbol.Name);

		if (!valueSymbols.ContainsKey(functionSymbol))
			throw new InvalidOperationException($"External function '{functionName}' not forward declared");
		
		var function = valueSymbols[functionSymbol].value;
		
		if (function == OpaqueValue.NullPtr || LLVM.IsUndef(function) == LLVMBool.True)
			throw new InvalidOperationException($"Unable to resolve external function '{functionSymbol.Name}'");

		var arguments = new LLVMOpaqueValue*[expression.arguments.Length];
		for (var i = 0; i < arguments.Length; i++)
		{
			arguments[i] = expression.arguments[i].Accept(this).value;
		}
		
		var functionType = LLVM.GlobalGetValueType(function);
		var returnType = LLVM.GetReturnType(functionType);
		var name = returnType == LLVM.VoidTypeInContext(currentContext)
			? ""
			: functionName;

		return new OpaqueValue(BuildCall(function, arguments, name));
	}

	public OpaqueValue Visit(ResolvedThisExpression expression)
	{
		if (!CurrentFunctionContext.hasThisRef)
			throw new InvalidOperationException("'this' is not valid in the current function context");

		return new OpaqueValue(LLVM.GetFirstParam(CurrentFunctionContext.functionValue));
	}

	public OpaqueValue Visit(ResolvedVarSymbolExpression symbolExpression)
	{
		if (!valueSymbols.ContainsKey(symbolExpression.varSymbol))
			throw new InvalidOperationException($"Variable '{symbolExpression.varSymbol.Name}' is invalid");
		
		var pointer = valueSymbols[symbolExpression.varSymbol].value;
		
		if (CurrentIsLValue)
			return new OpaqueValue(pointer);

		var type = symbolExpression.varSymbol.EvaluatedType!;
		return new OpaqueValue(BuildLoad(GetType(type), pointer, "", GetSize(type)));
	}

	public OpaqueValue Visit(ResolvedParameterSymbolExpression symbolExpression)
	{
		var param = CurrentFunctionContext.parameterPointers[symbolExpression.parameterSymbol].value;

		if (CurrentIsLValue)
			return new OpaqueValue(param);

		return new OpaqueValue(BuildLoad(GetType(symbolExpression.Type), param, symbolExpression.parameterSymbol.Name,
			GetSize(symbolExpression.Type!)));
	}

	public OpaqueValue Visit(ResolvedFieldExpression expression)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedConstExpression expression)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedTypeSymbolExpression symbolExpression)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedImportGroupingSymbolExpression symbolExpression)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedLiteralExpression expression)
	{
		//LLVM.ConstInt(LLVM.Int32Type(), statement.value.Accept(this), 1)
		// Todo: Handle signed values correctly, add other value types
		return expression.token.Value switch
		{
			byte value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int8TypeInContext(currentContext), value, LLVMBool.False)),
			
			sbyte value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int8TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			ushort value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int16TypeInContext(currentContext), value, LLVMBool.False)),
			
			short value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int16TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			uint value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int32TypeInContext(currentContext), value, LLVMBool.False)),
			
			int value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int32TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			ulong value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int64TypeInContext(currentContext), value, LLVMBool.False)),
			
			long value => 
				new OpaqueValue(LLVM.ConstInt(LLVM.Int64TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			float value => 
				new OpaqueValue(LLVM.ConstReal(LLVM.FloatTypeInContext(currentContext), value)),
			
			double value => 
				new OpaqueValue(LLVM.ConstReal(LLVM.DoubleTypeInContext(currentContext), value)),
			
			byte[] value => value.Length == 4
				? new OpaqueValue(DefineCharLiteral(value))
				: throw new InvalidOperationException("Char must be exactly 4 bytes long"),
			
			string value =>
				new OpaqueValue(DefineStringLiteral(value)),
			
			_ => throw new InvalidOperationException("Unsupported literal type")
		};

		LLVMOpaqueValue* DefineCharLiteral(byte[] value)
		{
			var elementType = LLVM.Int8TypeInContext(currentContext);
			var bytes = new LLVMOpaqueValue*[value.Length];
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = LLVM.ConstInt(elementType, value[i], LLVMBool.False);
			}

			return LLVM.ConstArray(elementType, ConvertArrayToPointer(bytes), (uint)value.LongLength);
		}

		LLVMOpaqueValue* DefineStringLiteral(string value)
		{
			if (constantLiterals.TryGetValue(value, out var existingConstant))
				return existingConstant.value;
			
			var charArray = ConvertUnicodeString(value, out var length);
			var stringType = LLVM.ArrayType(GetNativeType(TypeSymbol.UInt8), length);
			var stringRef = LLVM.AddGlobal(currentModule, stringType, EmptyString);
			LLVM.SetInitializer(stringRef,
				LLVM.ConstStringInContext(currentContext, charArray, length, LLVMBool.True));
			LLVM.SetGlobalConstant(stringRef, LLVMBool.True);
			LLVM.SetLinkage(stringRef, LLVMLinkage.LLVMPrivateLinkage);
			LLVM.SetUnnamedAddress(stringRef, LLVMUnnamedAddr.LLVMGlobalUnnamedAddr);
			LLVM.SetAlignment(stringRef, 1u);
			
			// Todo: Handle native pointer sizes
			var zeroIndex = LLVM.ConstInt(LLVM.Int64TypeInContext(currentContext), 0uL, LLVMBool.True);
			
			// https://llvm.org/docs/GetElementPtr.html#why-is-the-extra-0-index-required
			var indices = ConvertArrayToPointer(new LLVMOpaqueValue*[] { zeroIndex, zeroIndex });
			var gep = LLVM.BuildInBoundsGEP2(currentBuilder, stringType, stringRef, indices, 2, EmptyString);
			constantLiterals.Add(value, new OpaqueValue(gep));

			return gep;
		}
	}

	public OpaqueValue Visit(ResolvedBinaryExpression expression)
	{
		lvalueStack.Push(false);
		var left = expression.left.Accept(this).value;
		var right = expression.right.Accept(this).value;
		lvalueStack.Pop();
		
		var operatorSymbol = expression.operatorSymbol;

		if (operatorSymbol.IsNative)
		{
			if (expression.operation == BinaryExpression.Operation.Add)
			{
				var stringType = TypeSymbol.String.EvaluatedType;
				if (Type.Matches(expression.left.Type, stringType) && Type.Matches(expression.right.Type, stringType))
				{
					return BuildStringConcatenation(left, right);
				}
			}

			var isFloatingPoint = false;

			switch (expression.left.Type)
			{
				case BaseType type:
					if (type.typeSymbol == TypeSymbol.Float32)
						isFloatingPoint = true;
					else if (type.typeSymbol == TypeSymbol.Float64)
						isFloatingPoint = true;
					else if (type.typeSymbol == TypeSymbol.Float128)
						isFloatingPoint = true;
					
					break;
			}

			if (!isFloatingPoint)
			{
				switch (expression.right.Type)
				{
					case BaseType type:
						if (type.typeSymbol == TypeSymbol.Float32)
							isFloatingPoint = true;
						else if (type.typeSymbol == TypeSymbol.Float64)
							isFloatingPoint = true;
						else if (type.typeSymbol == TypeSymbol.Float128)
							isFloatingPoint = true;
						
						break;
				}
			}

			var resultValue = expression.operation switch
			{
				BinaryExpression.Operation.Add => isFloatingPoint
					? LLVM.BuildFAdd(currentBuilder, left, right, EmptyString)
					: LLVM.BuildAdd(currentBuilder, left, right, EmptyString),
				
				BinaryExpression.Operation.Multiply => isFloatingPoint
					? LLVM.BuildFMul(currentBuilder, left, right, EmptyString)
					: LLVM.BuildMul(currentBuilder, left, right, EmptyString),
				
				_ => throw new InvalidOperationException("Unsupported operation for native types")
			};

			if (!CurrentIsLValue)
				return new OpaqueValue(resultValue);

			var nativeAllocation = LLVM.BuildAlloca(currentBuilder, GetType(expression.Type), EmptyString);
			LLVM.BuildStore(currentBuilder, resultValue, nativeAllocation);
			return new OpaqueValue(nativeAllocation);
		}

		if (!valueSymbols.ContainsKey(operatorSymbol))
			throw new Exception($"Operator '{operatorSymbol.Name}' not forward declared");
		
		var function = valueSymbols[operatorSymbol].value;
		
		if (function == OpaqueValue.NullPtr || LLVM.IsUndef(function) == LLVMBool.True)
			throw new InvalidOperationException($"Unable to resolve external function '{operatorSymbol.Name}'");

		var arguments = new[]
		{
			left,
			right
		};

		var call = BuildCall(function, arguments, "");

		if (!CurrentIsLValue)
			return new OpaqueValue(call);

		var size = GetSize(expression.Type!);
		var allocation = BuildAlloca(GetType(expression.Type!), "", size);
		BuildStore(call, allocation, size);
		return new OpaqueValue(allocation);
	}

	private OpaqueValue BuildStringConcatenation(LLVMOpaqueValue* left, LLVMOpaqueValue* right)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedSymbolExpression expression)
	{
		throw new NotImplementedException();
	}

	public OpaqueValue Visit(ResolvedTypeAccessExpression expression)
	{
		switch (expression.target)
		{
			default:
				throw new NotImplementedException();
			
			case ConstructorSymbol symbol:
				return valueSymbols[symbol];
		}
	}

	public OpaqueValue Visit(ResolvedValueAccessExpression expression)
	{
		var (index, name) = expression.target switch
		{
			FieldSymbol fieldSymbol => (fieldSymbol.Index, fieldSymbol.Name),
			_ => throw new InvalidOperationException("Value access target is not valid")
		};

		lvalueStack.Push(true);
		var source = expression.source.Accept(this).value;
		lvalueStack.Pop();
		
		var sourceType = LLVM.TypeOf(source);
		if (sourceType != LLVM.PointerTypeInContext(currentContext, 0u))
		{
			throw new InvalidOperationException("Value access target must be a reference");
		}

		var loadedName = name;
		if (Debug)
		{
			loadedName = expression.source switch
			{
				ResolvedThisExpression => $"this.{name}",
				ResolvedVarSymbolExpression resolvedExpression => resolvedExpression.varSymbol.Name + $".{name}",
				ResolvedFieldExpression resolvedExpression => resolvedExpression.fieldSymbol.Name + $".{name}",
				ResolvedConstExpression resolvedExpression => resolvedExpression.constSymbol.Name + $".{name}",
				ResolvedParameterSymbolExpression resolvedExpression => resolvedExpression.parameterSymbol.Name + 
				                                                        $".{name}",
				_ => loadedName
			};
		}

		var type = GetType(expression.source.Type);
		var elementPtr = BuildStructGEP(type, source, index, $"{loadedName}.addr");
		
		if (CurrentIsLValue)
			return new OpaqueValue(elementPtr);

		var targetType = GetType(expression.target.EvaluatedType!);
		var targetSize = GetSize(expression.target.EvaluatedType!);
		return new OpaqueValue(BuildLoad(targetType, elementPtr, loadedName, targetSize));
	}

	public OpaqueValue Visit(ResolvedAssignmentExpression expression)
	{
		lvalueStack.Push(true);
		var left = expression.left.Accept(this).value;
		lvalueStack.Pop();
		
		lvalueStack.Push(false);
		var right = expression.right.Accept(this).value;
		lvalueStack.Pop();

		var size = GetSize(expression.left.Type!);
		BuildStore(right, left, size);
		if (!CurrentIsLValue)
			return new OpaqueValue(BuildLoad(GetType(expression.left.Type), left, "", size));

		return new OpaqueValue(left);
	}
}