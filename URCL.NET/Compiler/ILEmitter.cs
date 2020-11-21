using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace URCL.NET.Compiler
{
    public class ILEmitter
    {
        private static readonly ConstructorInfo StackInit = typeof(Stack<uint>).GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo PushStack = typeof(Stack<uint>).GetMethod(nameof(Stack<uint>.Push));
        private static readonly MethodInfo PopStack = typeof(Stack<uint>).GetMethod(nameof(Stack<uint>.Pop));

        private static readonly MethodInfo GetTime = typeof(Environment).GetProperty(nameof(Environment.TickCount)).GetMethod;

        private static readonly MethodInfo WriteValue = typeof(Console).GetMethods()
            .Where(m => m.Name == nameof(Console.WriteLine) &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(ulong)).First();
        private static readonly MethodInfo WriteChar = typeof(Console).GetMethods()
            .Where(m => m.Name == nameof(Console.Write) &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(char)).First();
        private static readonly MethodInfo ReadChar = typeof(Console).GetMethods()
            .Where(m => m.Name == nameof(Console.ReadKey) && m.GetParameters().Length == 1).First();
        private static readonly FieldInfo KeyValue = typeof(ConsoleKeyInfo).GetField(nameof(ConsoleKeyInfo.KeyChar));

        private LocalBuilder[] Registers;
        private FieldBuilder Memory;
        private FieldBuilder ValueStack;
        private FieldBuilder BenchmarkValue = null;

        public void Emit(string name, IEnumerable<UrclInstruction> instructions)
        {
            var labels = new Dictionary<Label, System.Reflection.Emit.Label>();

            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.RunAndCollect);
            var module = assembly.DefineDynamicModule(name);
            var type = module.DefineType("Program");

            Memory = type.DefineField(nameof(Memory), typeof(uint[]), FieldAttributes.Static);
            ValueStack = type.DefineField(nameof(ValueStack), typeof(Stack<ulong>), FieldAttributes.Static);

            var method = type.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static);
            method.SetReturnType(typeof(void));
            var il = new DebuggableEmitter(method.GetILGenerator());

            Console.WriteLine("FLAGS");
            il.DeclareLocal(typeof(ulong));

            Console.WriteLine("STACK");
            il.Emit(OpCodes.Newobj, StackInit);
            il.Emit(OpCodes.Stsfld, ValueStack);

            bool halted = false;
            foreach (var inst in instructions)
            {
                Console.WriteLine(inst);
                halted = false;

                switch (inst.Operation)
                {
                    case Operation.NOP:
                        il.Emit(OpCodes.Nop);
                        break;
                    case Operation.BRK:
                        il.Emit(OpCodes.Break);
                        break;
                    case Operation.HLT:
                        halted = true;
                        il.Emit(OpCodes.Ret);
                        break;
                    case Operation.ADD:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Add);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.INC:
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Ldc_I8, (ulong)1);
                        il.Emit(OpCodes.Add);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.SUB:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Sub);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.DEC:
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Ldc_I8, (ulong)1);
                        il.Emit(OpCodes.Sub);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.MLT:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Mul);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.DIV:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Div_Un);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.MOD:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Rem_Un);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.AND:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.And);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.OR:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Or);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.XOR:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Xor);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.NAND:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.And);
                        il.Emit(OpCodes.Not);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.NOR:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Or);
                        il.Emit(OpCodes.Not);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.XNOR:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Xor);
                        il.Emit(OpCodes.Not);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.NOT:
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Not);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.LSH:
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Shl);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.BSL:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Conv_I4);
                        il.Emit(OpCodes.Shl);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.RSH:
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Shr);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.BSR:
                        PushOperand(il, 1, inst);
                        PushOperand(il, 2, inst);
                        il.Emit(OpCodes.Conv_I4);
                        il.Emit(OpCodes.Shr);
                        UpdateFlags(il);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.MOV:
                        PushOperand(il, 1, inst);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.IMM:
                        PushOperand(il, 1, inst);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.LOAD:
                        il.Emit(OpCodes.Ldsfld, Memory);
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Ldelem, typeof(uint));
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.STORE:
                        il.Emit(OpCodes.Ldsfld, Memory);
                        PushOperand(il, 0, inst);
                        PushOperand(il, 1, inst);
                        il.Emit(OpCodes.Stelem, typeof(uint));
                        break;
                    case Operation.IN:
                        UseIO(il, inst.B, true);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.OUT:
                        PushOperand(il, 1, inst);
                        UseIO(il, inst.A, false);
                        break;
                    case Operation.PSH:
                        il.Emit(OpCodes.Ldsfld, ValueStack);
                        PushOperand(il, 0, inst);
                        il.Emit(OpCodes.Call, PushStack);
                        break;
                    case Operation.POP:
                        il.Emit(OpCodes.Ldsfld, ValueStack);
                        il.Emit(OpCodes.Call, PopStack);
                        PopOperand(il, 0, inst);
                        break;
                    case Operation.BRA:
                        il.Emit(OpCodes.Br, labels[inst.ALabel]);
                        break;
                    case Operation.BRZ:
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Brfalse, labels[inst.ALabel]);
                        break;
                    case Operation.BNZ:
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Brtrue, labels[inst.ALabel]);
                        break;
                    case Operation.BRC:
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Ldc_I8, (ulong)uint.MaxValue);
                        il.Emit(OpCodes.Bgt_Un, labels[inst.ALabel]);
                        break;
                    case Operation.BNC:
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Ldc_I8, (ulong)uint.MaxValue);
                        il.Emit(OpCodes.Ble_Un, labels[inst.ALabel]);
                        break;
                    case Operation.BRP:
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Ldc_I8, (ulong)int.MaxValue);
                        il.Emit(OpCodes.Ble_Un, labels[inst.ALabel]);
                        break;
                    case Operation.BRN:
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Conv_I4);
                        il.Emit(OpCodes.Conv_I8);
                        il.Emit(OpCodes.Ldc_I8, (long)(ulong)int.MaxValue);
                        il.Emit(OpCodes.Bgt_Un, labels[inst.ALabel]);
                        break;
                    case Operation.CAL:
                        throw new Exception("CAL is not currently supported when targeting .NET.");
                    case Operation.RET:
                        throw new Exception("RET is not currently supported when targeting .NET.");
                    case Operation.MINRAM:
                        il.Emit(OpCodes.Ldc_I8, inst.A);
                        il.Emit(OpCodes.Newarr, typeof(uint));
                        il.Emit(OpCodes.Stsfld, Memory);
                        break;
                    case Operation.BENCHMARK:
                        BenchmarkValue = type.DefineField(nameof(BenchmarkValue), typeof(int), FieldAttributes.Static);
                        il.Emit(OpCodes.Call, GetTime);
                        il.Emit(OpCodes.Stsfld, BenchmarkValue);
                        break;
                    case Operation.COMPILER_CREATELABEL:
                        labels.Add(inst.ALabel, il.DefineLabel());
                        break;
                    case Operation.COMPILER_MARKLABEL:
                        il.MarkLabel(labels[inst.ALabel]);
                        break;
                    case Operation.COMPILER_MAXREG:
                        Registers = new LocalBuilder[inst.A];
                        for (ulong i = 0; i < inst.A; i++)
                        {
                            Registers[i] = il.DeclareLocal(typeof(ulong));
                        }
                        break;
                    case Operation.COMPILER_PRAGMA: //TODO
                        break;
                    default:
                        throw new Exception($"Unimplemented instruction: \"{inst.Operation}\"");
                }
            }

            if (BenchmarkValue != null)
            {
                Console.WriteLine("BENCHMARK_FINISH");
                il.Emit(OpCodes.Call, GetTime);
                il.Emit(OpCodes.Ldsfld, BenchmarkValue);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Conv_I8);
                il.Emit(OpCodes.Call, WriteValue);
            }

            if (!halted)
            {
                Console.WriteLine("HLT");
                il.Emit(OpCodes.Ret);
            }

            type.CreateType();

            var data = new MemoryStream(new Lokad.ILPack.AssemblyGenerator().GenerateAssemblyBytes(assembly));

            var exportModule = Mono.Cecil.ModuleDefinition.ReadModule(data);
            exportModule.Assembly.MainModule.Kind = Mono.Cecil.ModuleKind.Console;
            exportModule.EntryPoint = exportModule.Types.Where(t => t.Name == "Program").First().Methods.Where(m => m.Name == "Main").First();

            exportModule.Write($"{name}.exe");
        }

        private static void UseIO(DebuggableEmitter il, ulong port, bool input)
        {
            switch ((UrclPort)port)
            {
                case UrclPort.ValueDisplay:
                case UrclPort.ValueDisplay32Dec:
                    if (input)
                    {
                        throw new Exception("Reading from output only port is not allowed.");
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I8, (ulong)uint.MaxValue);
                        il.Emit(OpCodes.And);
                        il.Emit(OpCodes.Call, WriteValue);
                    }
                    break;
                case UrclPort.Teletype:
                case UrclPort.Teletype8BitASCII:
                    if (input)
                    {
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Call, ReadChar);
                        il.Emit(OpCodes.Ldfld, KeyValue);
                        il.Emit(OpCodes.Conv_I1);
                    }
                    else
                    {
                        il.Emit(OpCodes.Conv_I1);
                        il.Emit(OpCodes.Call, WriteChar);
                    }
                    break;
                default:
                    throw new Exception($"Unsupported I/O port {port}.");
            }
        }

        private void PushOperand(DebuggableEmitter il, ulong index, UrclInstruction inst)
        {
            bool isImm;
            ulong value;

            switch (index)
            {
                case 0:
                    isImm = inst.AType switch
                    {
                        OperandType.Register => false,
                        OperandType.Immediate => true,
                        _ => throw new InvalidOperationException(),
                    };
                    value = inst.A;
                    break;
                case 1:
                    isImm = inst.BType switch
                    {
                        OperandType.Register => false,
                        OperandType.Immediate => true,
                        _ => throw new InvalidOperationException(),
                    };
                    value = inst.B;
                    break;
                case 2:
                    isImm = inst.CType switch
                    {
                        OperandType.Register => false,
                        OperandType.Immediate => true,
                        _ => throw new InvalidOperationException(),
                    };
                    value = inst.C;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (isImm)
            {
                il.Emit(OpCodes.Ldc_I8, value);
            }
            else
            {
                if (value == 0)
                {
                    il.Emit(OpCodes.Ldc_I8, 0UL);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, Registers[value - 1]);
                }
            }
        }

        private void PopOperand(DebuggableEmitter il, ulong index, UrclInstruction inst)
        {
            ulong reg;

            switch (index)
            {
                case 0:
                    if (inst.AType == OperandType.Register)
                    {
                        reg = inst.A;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    break;
                case 1:
                    if (inst.AType == OperandType.Register)
                    {
                        reg = inst.B;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    break;
                case 2:
                    if (inst.AType == OperandType.Register)
                    {
                        reg = inst.C;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (reg == 0)
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.Emit(OpCodes.Stloc, Registers[reg - 1]);
            }
        }

        private static void UpdateFlags(DebuggableEmitter il)
        {
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldc_I8, uint.MaxValue);
            il.Emit(OpCodes.And);
        }

        private class DebuggableEmitter
        {
            private ILGenerator IL;

            public DebuggableEmitter(ILGenerator il)
            {
                IL = il;
            }

            public void Emit(OpCode opcode)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType})");
                IL.Emit(opcode);
            }

            public void Emit(OpCode opcode, ConstructorInfo con)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {con.GetType()} {con}");
                IL.Emit(opcode, con);
            }

            public void Emit(OpCode opcode, byte arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, arg);
            }

            public void Emit(OpCode opcode, double arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, arg);
            }

            public void Emit(OpCode opcode, FieldInfo field)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {field.GetType()} {field}");
                IL.Emit(opcode, field);
            }

            public void Emit(OpCode opcode, float arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, arg);
            }

            public void Emit(OpCode opcode, int arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, arg);
            }

            public void Emit(OpCode opcode, LocalBuilder local)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {local.GetType()} {local}");
                IL.Emit(opcode, local);
            }

            public void Emit(OpCode opcode, long arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, arg);
            }

            public void Emit(OpCode opcode, ulong arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, (long)arg);
            }

            public void Emit(OpCode opcode, MethodInfo meth)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {meth.GetType()} {meth}");
                IL.Emit(opcode, meth);
            }

            public void Emit(OpCode opcode, short arg)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {arg.GetType()} {arg}");
                IL.Emit(opcode, arg);
            }

            public void Emit(OpCode opcode, SignatureHelper signature)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {signature.GetType()} {signature}");
                IL.Emit(opcode, signature);
            }

            public void Emit(OpCode opcode, string str)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {str.GetType()} {str}");
                IL.Emit(opcode, str);
            }

            public void Emit(OpCode opcode, System.Reflection.Emit.Label label)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {label.GetType()} {label}");
                IL.Emit(opcode, label);
            }

            public void Emit(OpCode opcode, System.Reflection.Emit.Label[] labels)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {labels.GetType()} {labels}");
                IL.Emit(opcode, labels);
            }

            public void Emit(OpCode opcode, Type cls)
            {
                Console.WriteLine($"\t{opcode.Value:X} ({opcode.OpCodeType}) {cls.GetType()} {cls}");
                IL.Emit(opcode, cls);
            }

            public LocalBuilder DeclareLocal(Type localType)
            {
                Console.WriteLine($"\tLocal {localType}");
                return IL.DeclareLocal(localType);
            }

            public LocalBuilder DeclareLocal(Type localType, bool pinned)
            {
                Console.WriteLine($"\tLocal {localType} (Pinned)");
                return IL.DeclareLocal(localType, pinned);
            }

            public System.Reflection.Emit.Label DefineLabel()
            {
                return IL.DefineLabel();
            }

            public void MarkLabel(System.Reflection.Emit.Label loc)
            {
                IL.MarkLabel(loc);
            }
        }
    }
}
