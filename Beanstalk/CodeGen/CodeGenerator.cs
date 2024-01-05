using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Beanstalk.Analysis.Semantics;
using LLVMSharp.Interop;
using ReferenceType = Beanstalk.Analysis.Semantics.ReferenceType;
using Type = Beanstalk.Analysis.Semantics.Type;

namespace Beanstalk.CodeGen;

public readonly struct LLVMBool
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

public readonly unsafe struct Value
{
	public readonly LLVMOpaqueValue* value;

	public static readonly LLVMOpaqueValue* NullPtr = (LLVMOpaqueValue*)IntPtr.Zero;
	public static readonly Value Null = new(NullPtr);

	public Value(LLVMOpaqueValue* value)
	{
		this.value = value;
	}
}

public readonly unsafe struct OpaqueType
{
	public readonly LLVMOpaqueType* value;

	public static readonly LLVMOpaqueType* NullPtr = (LLVMOpaqueType*)IntPtr.Zero;
	public static readonly OpaqueType Null = new(NullPtr);

	public OpaqueType(LLVMOpaqueType* value)
	{
		this.value = value;
	}
}

public unsafe class CodeGenerator : ResolvedStatementNode.IVisitor, ResolvedExpressionNode.IVisitor<Value>
{
	private enum CodeGenerationPass
	{
		TopLevelDeclarations,
		MemberDeclarations,
		Definitions,
		Complete
	}
	
	private LLVMOpaqueModule* currentModule;
	private LLVMOpaqueContext* currentContext;
	private LLVMOpaqueBuilder* currentBuilder;
	private CodeGenerationPass currentPass;
	private ISymbol? currentFunction;
	private readonly Dictionary<ISymbol, Value> valueSymbols = new(); 
	private readonly Dictionary<ISymbol, OpaqueType> typeSymbols = new(); 

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
		LLVM.InitializeAllTargets();
		
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
			valueSymbols.Clear();
			typeSymbols.Clear();
			
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

			LLVM.DumpModule(currentModule);

			var outputBitCodePath = Path.Combine(objDirectory, Path.ChangeExtension(relativePath, ".bc"));
			LLVM.WriteBitcodeToFile(currentModule, ConvertString(outputBitCodePath));
			sourceFiles.Add(outputBitCodePath);
		}
		
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
			return LLVM.Int8TypeInContext(currentContext);
		
		// Todo: This is not correct
		if (nativeSymbol == TypeSymbol.String)
			return LLVM.PointerType(LLVM.Int8TypeInContext(currentContext), 0u);

		return LLVM.VoidTypeInContext(currentContext);
	}

	public void Visit(ResolvedProgramStatement programStatement)
	{
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
		LLVMOpaqueType* structType;
		var structSymbol = structDeclarationStatement.structSymbol;

		switch (currentPass)
		{
			case CodeGenerationPass.TopLevelDeclarations:
			{
				structType = LLVM.StructCreateNamed(currentContext,
					ConvertString(structSymbol.Name));

				typeSymbols.Add(structSymbol, new OpaqueType(structType));
				break;
			}

			case CodeGenerationPass.MemberDeclarations:
			{
				if (!typeSymbols.ContainsKey(structSymbol))
					throw new Exception($"Struct '{structSymbol.Name}' " +
					                    $"not forward declared");

				structType = typeSymbols[structSymbol].value;

				var elementTypeList = new List<Type>();
				foreach (var statement in structDeclarationStatement.statements)
				{
					switch (statement)
					{
						case ResolvedFieldDeclarationStatement fieldDeclarationStatement:
							elementTypeList.Add(fieldDeclarationStatement.fieldSymbol.EvaluatedType!);
							break;
						
						case ResolvedConstructorDeclarationStatement constructorDeclarationStatement:
							var constructorSymbol = constructorDeclarationStatement.constructorSymbol;
							var parameterList = new List<OpaqueType>
							{
								new(GetType(constructorSymbol.This.EvaluatedType))
							};

							foreach (var parameter in constructorSymbol.Parameters)
							{
								parameterList.Add(new OpaqueType(GetType(parameter.EvaluatedType)));
							}

							var parameterTypes = new LLVMOpaqueType*[parameterList.Count];
							for (var i = 0; i < parameterTypes.Length; i++)
							{
								parameterTypes[i] = parameterList[i].value;
							}

							var constructorType = LLVM.FunctionType(structType, ConvertArrayToPointer(parameterTypes),
								(uint)parameterTypes.LongLength, LLVMBool.False);

							var constructor = LLVM.AddFunction(currentModule,
								ConvertString($"{structSymbol.Name}.new"), constructorType);

							valueSymbols.Add(constructorSymbol, new Value(constructor));
							break;
					}
				}

				var elementTypes = new LLVMOpaqueType*[elementTypeList.Count];
				for (var i = 0; i < elementTypes.Length; i++)
				{
					elementTypes[i] = GetType(elementTypeList[i]);
				}

				LLVM.StructSetBody(structType, ConvertArrayToPointer(elementTypes), (uint)elementTypes.LongLength,
					LLVMBool.False);

				structType = typeSymbols[structSymbol].value;
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
		var entryPoint = LLVM.AddFunction(currentModule, ConvertString("main"),
			LLVM.FunctionType(LLVM.Int32Type(), (LLVMOpaqueType**)IntPtr.Zero, 0u, LLVMBool.False));
		
		// Positioning
		var entryBody = LLVM.AppendBasicBlockInContext(currentContext, entryPoint, ConvertString("entry"));
		LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		
		// Instructions
		foreach (var statement in entryStatement.statements)
		{
			statement.Accept(this);
			LLVM.PositionBuilderAtEnd(currentBuilder, entryBody);
		}
		
		// Verification
		if (LLVM.VerifyFunction(entryPoint, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(entryPoint);
	}

	public void Visit(ResolvedExternalFunctionStatement statement)
	{
		if (currentPass != CodeGenerationPass.TopLevelDeclarations)
			return;
		
		var parameterList = statement.externalFunctionSymbol.Parameters;
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
		var returnType = GetType(statement.externalFunctionSymbol.ReturnType);
	
		var functionType = LLVM.FunctionType(returnType, paramTypes, (uint)parameterTypes.Length, isVariadic);
		var name = statement.externalFunctionSymbol.Attributes.GetValueOrDefault("entry",
			statement.externalFunctionSymbol.Name);
		
		var externalFunction = LLVM.AddFunction(currentModule, ConvertString(name),
			functionType);
		
		valueSymbols.Add(statement.externalFunctionSymbol, new Value(externalFunction));
	
		// Verification
		if (LLVM.VerifyFunction(externalFunction, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(externalFunction);
	}

	public void Visit(ResolvedFunctionDeclarationStatement statement)
	{
		throw new NotImplementedException();
	}

	public void Visit(ResolvedConstructorDeclarationStatement statement)
	{
		if (currentPass != CodeGenerationPass.Definitions)
			return;
		
		var constructorSymbol = statement.constructorSymbol;

		if (!valueSymbols.ContainsKey(constructorSymbol))
			throw new Exception($"Constructor for type '{constructorSymbol.Owner.Name}' not forward declared");
		
		var constructor = valueSymbols[constructorSymbol].value;
		
		// Todo: Function not found
		if (constructor == Value.NullPtr || LLVM.IsUndef(constructor) == LLVMBool.True)
			return;
		
		// Body
		currentFunction = constructorSymbol;
		statement.body.Accept(this);
		currentFunction = null;
	
		// Verification
		if (LLVM.VerifyFunction(constructor, LLVMVerifierFailureAction.LLVMAbortProcessAction) != 0)
			LLVM.InstructionEraseFromParent(constructor);
	}

	public void Visit(ResolvedDestructorDeclarationStatement statement)
	{
		throw new NotImplementedException();
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

	public void Visit(ResolvedSimpleStatement statement)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedFunctionExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedConstructorExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedExternalFunctionExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedFunctionCallExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedConstructorCallExpression expression)
	{
		var ownerSymbol = expression.constructorSymbol.Owner;
		var constructorSymbol = expression.constructorSymbol;

		if (!valueSymbols.ContainsKey(constructorSymbol))
			throw new Exception($"Constructor for type '{constructorSymbol.Owner.Name}' not forward declared");

		if (!typeSymbols.ContainsKey(ownerSymbol))
			throw new Exception($"Type '{ownerSymbol.Name}' not forward declared");
		
		var owner = typeSymbols[ownerSymbol].value;
		var constructor = valueSymbols[constructorSymbol].value;
		
		// Todo: Function not found
		if (constructor == Value.NullPtr || LLVM.IsUndef(constructor) == LLVMBool.True)
			return Value.Null;
		
		// Allocate memory
		// Todo: Memory management!
		var allocation = LLVM.BuildAlloca(currentBuilder, owner, ConvertString(""));

		var arguments = new LLVMOpaqueValue*[expression.arguments.Length + 1];
		for (var i = 1; i < arguments.Length; i++)
		{
			arguments[i] = expression.arguments[i - 1].Accept(this).value;
		}

		arguments[0] = LLVM.BuildIntToPtr(currentBuilder, allocation, LLVM.PointerType(owner, 0u), ConvertString(""));
		
		var constructorType = LLVM.GlobalGetValueType(constructor);
		var args = ConvertArrayToPointer(arguments);
		var argc = (uint)arguments.Length;
		var name = ConvertString("");

		var call = LLVM.BuildCall2(currentBuilder, constructorType, constructor, args, argc, name);
		return new Value(call);
	}

	public Value Visit(ResolvedExternalFunctionCallExpression expression)
	{
		var functionSymbol = expression.functionSymbol;
		var functionName = functionSymbol.Attributes.GetValueOrDefault("entry", functionSymbol.Name);

		if (!valueSymbols.ContainsKey(functionSymbol))
			throw new Exception($"External function '{functionName}' not forward declared");
		
		var function = valueSymbols[functionSymbol].value;
		
		// Todo: Function not found
		if (function == Value.NullPtr || LLVM.IsUndef(function) == LLVMBool.True)
			return Value.Null;

		var arguments = new LLVMOpaqueValue*[expression.arguments.Length];
		for (var i = 0; i < arguments.Length; i++)
		{
			arguments[i] = expression.arguments[i].Accept(this).value;
		}
		
		var functionType = LLVM.GlobalGetValueType(function);
		var returnType = LLVM.GetReturnType(functionType);
		var args = ConvertArrayToPointer(arguments);
		var argc = (uint)arguments.Length;
		var name = returnType == LLVM.VoidTypeInContext(currentContext)
			? ConvertString("")
			: ConvertString(functionName);

		var call = LLVM.BuildCall2(currentBuilder, functionType, function, args, argc, name);
		return new Value(call);
	}

	public Value Visit(ResolvedThisExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedVarExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedFieldExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedConstExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedTypeExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedLiteralExpression expression)
	{
		//LLVM.ConstInt(LLVM.Int32Type(), statement.value.Accept(this), 1)
		// Todo: Handle signed values correctly, add other value types
		return expression.token.Value switch
		{
			byte value => 
				new Value(LLVM.ConstInt(LLVM.Int8TypeInContext(currentContext), value, LLVMBool.False)),
			
			sbyte value => 
				new Value(LLVM.ConstInt(LLVM.Int8TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			ushort value => 
				new Value(LLVM.ConstInt(LLVM.Int16TypeInContext(currentContext), value, LLVMBool.False)),
			
			short value => 
				new Value(LLVM.ConstInt(LLVM.Int16TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			uint value => 
				new Value(LLVM.ConstInt(LLVM.Int32TypeInContext(currentContext), value, LLVMBool.False)),
			
			int value => 
				new Value(LLVM.ConstInt(LLVM.Int32TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			ulong value => 
				new Value(LLVM.ConstInt(LLVM.Int64TypeInContext(currentContext), value, LLVMBool.False)),
			
			long value => 
				new Value(LLVM.ConstInt(LLVM.Int64TypeInContext(currentContext), (ulong)value, LLVMBool.True)),
			
			float value => 
				new Value(LLVM.ConstReal(LLVM.FloatTypeInContext(currentContext), value)),
			
			double value => 
				new Value(LLVM.ConstReal(LLVM.DoubleTypeInContext(currentContext), value)),
			
			string value =>
				new Value(DefineStringLiteral(value)),
			
			_ => Value.Null
		};

		LLVMOpaqueValue* DefineStringLiteral(string value)
		{
			var charArray = ConvertUnicodeString(value, out var length);
			var stringType = LLVM.ArrayType(GetNativeType(TypeSymbol.Char), length);
			var stringRef = LLVM.AddGlobal(currentModule, stringType, ConvertString(""));
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
			var gep = LLVM.BuildInBoundsGEP2(currentBuilder, stringType, stringRef, indices, 2, ConvertString(""));

			return gep;
		}
	}

	public Value Visit(ResolvedSymbolExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedTypeAccessExpression expression)
	{
		switch (expression.target)
		{
			default:
				throw new NotImplementedException();
			
			case ConstructorSymbol symbol:
				return valueSymbols[symbol];
		}
	}

	public Value Visit(ResolvedValueAccessExpression expression)
	{
		throw new NotImplementedException();
	}

	public Value Visit(ResolvedAssignmentExpression expression)
	{
		if (!valueSymbols.TryGetValue(expression.left, out var symbol))
			throw new Exception($"Unable to resolve symbol '{expression.left.Name}'");

		var value = expression.right.Accept(this);
		return new Value(LLVM.BuildStore(currentBuilder, value.value, symbol.value));
	}
}