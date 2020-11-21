using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace URCL.NET
{
    class MethodCompiler
    {
        public TypeCompiler Type { get; }
        public string Name => Method.Name;
        public int Args => Method == null ? 0 : Method.Parameters.Count;
        public int Locals => Method == null ? 0 : Method.Body.Variables.Count;
        public UrclInstruction[] Body => Instructions.ToArray();
        public IEnumerable<Label> Labels => MyLabels.Keys.Append(MethodLabel);
        public Label MethodLabel { get; }
        public Dictionary<int, List<string>> Comments { get; } = new Dictionary<int, List<string>>();

        private readonly Configuration Configuration;
        private readonly MethodDefinition Method;
        private readonly List<UrclInstruction> Instructions = new List<UrclInstruction>();
        private readonly Dictionary<Label, int> MyLabels = new Dictionary<Label, int>();

        public MethodCompiler(Configuration configuration, TypeCompiler type, MethodDefinition method)
        {
            Configuration = configuration;
            Type = type;
            Method = method;
            MethodLabel = CreateLabel();
        }

        public void PreCompile() { }

        public void Resolve()
        {
            Comment($"{Method} - {Method.Body.Instructions.Count} IL instructions");

            MarkLabel(MethodLabel);

            var localLabels = Method.Body.Instructions.Select(inst => CreateLabel()).ToArray();
            var labelIndex = 0;

            foreach (var op in Method.Body.Instructions)
            {
                if (op.OpCode.Code != Code.Nop || Configuration.EmitNop)
                {
                    Comment($"CIL: {op.OpCode.Code} {(op.Operand == null ? "<null>" : $"{op.Operand.GetType().FullName}: \"{op.Operand}\"")}");
                }

                MarkLabel(localLabels[labelIndex]);
                labelIndex++;

                switch (op.OpCode.Code)
                {
                    case Code.Nop:
                        if (Configuration.EmitNop)
                        {
                            Emit(Operation.NOP);
                        }
                        break;
                    case Code.Break:
                        if (Configuration.EmitBrk)
                        {
                            Emit(Operation.BRK);
                        }
                        break;
                    /*case Code.Ldarg_0:
                        break;
                    case Code.Ldarg_1:
                        break;
                    case Code.Ldarg_2:
                        break;
                    case Code.Ldarg_3:
                        break;
                    case Code.Ldloc_0:
                        break;
                    case Code.Ldloc_1:
                        break;
                    case Code.Ldloc_2:
                        break;
                    case Code.Ldloc_3:
                        break;
                    case Code.Stloc_0:
                        break;
                    case Code.Stloc_1:
                        break;
                    case Code.Stloc_2:
                        break;
                    case Code.Stloc_3:
                        break;
                    case Code.Ldarg_S:
                        break;
                    case Code.Ldarga_S:
                        break;
                    case Code.Starg_S:
                        break;
                    case Code.Ldloc_S:
                        break;
                    case Code.Ldloca_S:
                        break;
                    case Code.Stloc_S:
                        break;
                    case Code.Ldnull:
                        break;
                    case Code.Ldc_I4_M1:
                        break;
                    case Code.Ldc_I4_0:
                        break;
                    case Code.Ldc_I4_1:
                        break;
                    case Code.Ldc_I4_2:
                        break;
                    case Code.Ldc_I4_3:
                        break;
                    case Code.Ldc_I4_4:
                        break;
                    case Code.Ldc_I4_5:
                        break;
                    case Code.Ldc_I4_6:
                        break;
                    case Code.Ldc_I4_7:
                        break;
                    case Code.Ldc_I4_8:
                        break;
                    case Code.Ldc_I4_S:
                        break;
                    case Code.Ldc_I4:
                        break;
                    case Code.Ldc_I8:
                        break;
                    case Code.Ldc_R4:
                        break;
                    case Code.Ldc_R8:
                        break;
                    case Code.Dup:
                        break;
                    case Code.Pop:
                        break;
                    case Code.Jmp:
                        break;
                    case Code.Call:
                        break;
                    case Code.Calli:
                        break;
                    case Code.Ret:
                        break;
                    case Code.Br_S:
                        break;
                    case Code.Brfalse_S:
                        break;
                    case Code.Brtrue_S:
                        break;
                    case Code.Beq_S:
                        break;
                    case Code.Bge_S:
                        break;
                    case Code.Bgt_S:
                        break;
                    case Code.Ble_S:
                        break;
                    case Code.Blt_S:
                        break;
                    case Code.Bne_Un_S:
                        break;
                    case Code.Bge_Un_S:
                        break;
                    case Code.Bgt_Un_S:
                        break;
                    case Code.Ble_Un_S:
                        break;
                    case Code.Blt_Un_S:
                        break;
                    case Code.Br:
                        break;
                    case Code.Brfalse:
                        break;
                    case Code.Brtrue:
                        break;
                    case Code.Beq:
                        break;
                    case Code.Bge:
                        break;
                    case Code.Bgt:
                        break;
                    case Code.Ble:
                        break;
                    case Code.Blt:
                        break;
                    case Code.Bne_Un:
                        break;
                    case Code.Bge_Un:
                        break;
                    case Code.Bgt_Un:
                        break;
                    case Code.Ble_Un:
                        break;
                    case Code.Blt_Un:
                        break;
                    case Code.Switch:
                        break;
                    case Code.Ldind_I1:
                        break;
                    case Code.Ldind_U1:
                        break;
                    case Code.Ldind_I2:
                        break;
                    case Code.Ldind_U2:
                        break;
                    case Code.Ldind_I4:
                        break;
                    case Code.Ldind_U4:
                        break;
                    case Code.Ldind_I8:
                        break;
                    case Code.Ldind_I:
                        break;
                    case Code.Ldind_R4:
                        break;
                    case Code.Ldind_R8:
                        break;
                    case Code.Ldind_Ref:
                        break;
                    case Code.Stind_Ref:
                        break;
                    case Code.Stind_I1:
                        break;
                    case Code.Stind_I2:
                        break;
                    case Code.Stind_I4:
                        break;
                    case Code.Stind_I8:
                        break;
                    case Code.Stind_R4:
                        break;
                    case Code.Stind_R8:
                        break;
                    case Code.Add:
                        break;
                    case Code.Sub:
                        break;
                    case Code.Mul:
                        break;
                    case Code.Div:
                        break;
                    case Code.Div_Un:
                        break;
                    case Code.Rem:
                        break;
                    case Code.Rem_Un:
                        break;
                    case Code.And:
                        break;
                    case Code.Or:
                        break;
                    case Code.Xor:
                        break;
                    case Code.Shl:
                        break;
                    case Code.Shr:
                        break;
                    case Code.Shr_Un:
                        break;
                    case Code.Neg:
                        break;
                    case Code.Not:
                        break;
                    case Code.Conv_I1:
                        break;
                    case Code.Conv_I2:
                        break;
                    case Code.Conv_I4:
                        break;
                    case Code.Conv_I8:
                        break;
                    case Code.Conv_R4:
                        break;
                    case Code.Conv_R8:
                        break;
                    case Code.Conv_U4:
                        break;
                    case Code.Conv_U8:
                        break;
                    case Code.Callvirt:
                        break;
                    case Code.Cpobj:
                        break;
                    case Code.Ldobj:
                        break;
                    case Code.Ldstr:
                        break;
                    case Code.Newobj:
                        break;
                    case Code.Castclass:
                        break;
                    case Code.Isinst:
                        break;
                    case Code.Conv_R_Un:
                        break;
                    case Code.Unbox:
                        break;
                    case Code.Throw:
                        break;
                    case Code.Ldfld:
                        break;
                    case Code.Ldflda:
                        break;
                    case Code.Stfld:
                        break;
                    case Code.Ldsfld:
                        break;
                    case Code.Ldsflda:
                        break;
                    case Code.Stsfld:
                        break;
                    case Code.Stobj:
                        break;
                    case Code.Conv_Ovf_I1_Un:
                        break;
                    case Code.Conv_Ovf_I2_Un:
                        break;
                    case Code.Conv_Ovf_I4_Un:
                        break;
                    case Code.Conv_Ovf_I8_Un:
                        break;
                    case Code.Conv_Ovf_U1_Un:
                        break;
                    case Code.Conv_Ovf_U2_Un:
                        break;
                    case Code.Conv_Ovf_U4_Un:
                        break;
                    case Code.Conv_Ovf_U8_Un:
                        break;
                    case Code.Conv_Ovf_I_Un:
                        break;
                    case Code.Conv_Ovf_U_Un:
                        break;
                    case Code.Box:
                        break;
                    case Code.Newarr:
                        break;
                    case Code.Ldlen:
                        break;
                    case Code.Ldelema:
                        break;
                    case Code.Ldelem_I1:
                        break;
                    case Code.Ldelem_U1:
                        break;
                    case Code.Ldelem_I2:
                        break;
                    case Code.Ldelem_U2:
                        break;
                    case Code.Ldelem_I4:
                        break;
                    case Code.Ldelem_U4:
                        break;
                    case Code.Ldelem_I8:
                        break;
                    case Code.Ldelem_I:
                        break;
                    case Code.Ldelem_R4:
                        break;
                    case Code.Ldelem_R8:
                        break;
                    case Code.Ldelem_Ref:
                        break;
                    case Code.Stelem_I:
                        break;
                    case Code.Stelem_I1:
                        break;
                    case Code.Stelem_I2:
                        break;
                    case Code.Stelem_I4:
                        break;
                    case Code.Stelem_I8:
                        break;
                    case Code.Stelem_R4:
                        break;
                    case Code.Stelem_R8:
                        break;
                    case Code.Stelem_Ref:
                        break;
                    case Code.Ldelem_Any:
                        break;
                    case Code.Stelem_Any:
                        break;
                    case Code.Unbox_Any:
                        break;
                    case Code.Conv_Ovf_I1:
                        break;
                    case Code.Conv_Ovf_U1:
                        break;
                    case Code.Conv_Ovf_I2:
                        break;
                    case Code.Conv_Ovf_U2:
                        break;
                    case Code.Conv_Ovf_I4:
                        break;
                    case Code.Conv_Ovf_U4:
                        break;
                    case Code.Conv_Ovf_I8:
                        break;
                    case Code.Conv_Ovf_U8:
                        break;
                    case Code.Refanyval:
                        break;
                    case Code.Ckfinite:
                        break;
                    case Code.Mkrefany:
                        break;
                    case Code.Ldtoken:
                        break;
                    case Code.Conv_U2:
                        break;
                    case Code.Conv_U1:
                        break;
                    case Code.Conv_I:
                        break;
                    case Code.Conv_Ovf_I:
                        break;
                    case Code.Conv_Ovf_U:
                        break;
                    case Code.Add_Ovf:
                        break;
                    case Code.Add_Ovf_Un:
                        break;
                    case Code.Mul_Ovf:
                        break;
                    case Code.Mul_Ovf_Un:
                        break;
                    case Code.Sub_Ovf:
                        break;
                    case Code.Sub_Ovf_Un:
                        break;
                    case Code.Endfinally:
                        break;
                    case Code.Leave:
                        break;
                    case Code.Leave_S:
                        break;
                    case Code.Stind_I:
                        break;
                    case Code.Conv_U:
                        break;
                    case Code.Arglist:
                        break;
                    case Code.Ceq:
                        break;
                    case Code.Cgt:
                        break;
                    case Code.Cgt_Un:
                        break;
                    case Code.Clt:
                        break;
                    case Code.Clt_Un:
                        break;
                    case Code.Ldftn:
                        break;
                    case Code.Ldvirtftn:
                        break;
                    case Code.Ldarg:
                        break;
                    case Code.Ldarga:
                        break;
                    case Code.Starg:
                        break;
                    case Code.Ldloc:
                        break;
                    case Code.Ldloca:
                        break;
                    case Code.Stloc:
                        break;
                    case Code.Localloc:
                        break;
                    case Code.Endfilter:
                        break;
                    case Code.Unaligned:
                        break;
                    case Code.Volatile:
                        break;
                    case Code.Tail:
                        break;
                    case Code.Initobj:
                        break;
                    case Code.Constrained:
                        break;
                    case Code.Cpblk:
                        break;
                    case Code.Initblk:
                        break;
                    case Code.No:
                        break;
                    case Code.Rethrow:
                        break;
                    case Code.Sizeof:
                        break;
                    case Code.Refanytype:
                        break;
                    case Code.Readonly:
                        break;*/
                    default:
                        Unsupported(op.OpCode.Code);
                        break;
                }
            }
        }

        public void Branch(Label label)
        {
            Emit(Operation.BRA, label);
        }

        public void BranchIfZero(ulong register, Label label)
        {
            Emit(Operation.OR, OperandType.Register, register, OperandType.Register, register, OperandType.Immediate, 0);
            Emit(Operation.BRZ, label);
        }

        public void BranchIfNotZero(ulong register, Label label)
        {
            Emit(Operation.OR, OperandType.Register, register, OperandType.Register, register, OperandType.Immediate, 0);
            Emit(Operation.BNZ, label);
        }

        public void BranchIfPositive(ulong register, Label label)
        {
            Emit(Operation.OR, OperandType.Register, register, OperandType.Register, register, OperandType.Immediate, 0);
            Emit(Operation.BRP, label);
        }

        public void BranchIfNegative(ulong register, Label label)
        {
            Emit(Operation.OR, OperandType.Register, register, OperandType.Register, register, OperandType.Immediate, 0);
            Emit(Operation.BRN, label);
        }

        public Label CreateLabel()
        {
            var label = new Label();
            Emit(Operation.COMPILER_CREATELABEL, label);
            return label;
        }

        public void MarkLabel(Label label)
        {
            Emit(Operation.COMPILER_MARKLABEL, label);
        }

        public void Increment(ulong register)
        {
            Emit(Operation.INC, OperandType.Register, register, OperandType.Register, register);
        }

        public void Decrement(ulong register)
        {
            Emit(Operation.DEC, OperandType.Register, register, OperandType.Register, register);
        }

        public void Emit(Operation operation)
        {
            Instructions.Add(new UrclInstruction(operation));
        }

        public void Emit(Operation operation, OperandType type, ulong value)
        {
            Instructions.Add(new UrclInstruction(operation, type, value));
        }

        public void Emit(Operation operation, OperandType aType, ulong a, OperandType bType, ulong b)
        {
            Instructions.Add(new UrclInstruction(operation, aType, a, bType, b));
        }

        public void Emit(Operation operation, OperandType aType, ulong a, OperandType bType, ulong b, OperandType cType, ulong c)
        {
            Instructions.Add(new UrclInstruction(operation, aType, a, bType, b, cType, c));
        }

        public void Emit(Operation operation, Label label)
        {
            Instructions.Add(new UrclInstruction(operation, label));
        }

        public void Emit(Operation operation, OperandType aType, ulong a, Label label)
        {
            Instructions.Add(new UrclInstruction(operation, aType, a, label));
        }

        public void Emit(Operation operation, Label aLabel, Label bLabel)
        {
            Instructions.Add(new UrclInstruction(operation, aLabel, bLabel));
        }

        public void Emit(Operation operation, Label aLabel, Label bLabel, Label cLabel)
        {
            Instructions.Add(new UrclInstruction(operation, aLabel, bLabel, cLabel));
        }

        public void Comment(string comment)
        {
            if (Comments.TryGetValue(Instructions.Count, out List<string> comments))
            {
                comments.Add(comment);
            }
            else
            {
                Comments[Instructions.Count] = new List<string> { comment };
            }
        }

        private void Unsupported(Code code, object value)
        {
            string type = value is null ? "<null>" : value.GetType().FullName;

            throw new NotSupportedException($"CIL opcode {code} with \"{type}\" operand is not supported.");
        }

        private void Unsupported(Code code)
        {
            throw new NotSupportedException($"CIL opcode {code} is not supported.");
        }

        private void NotFound(MethodDefinition method)
        {
            throw new NotImplementedException($"{method.DeclaringType.FullName}:{method.Name} could not be found.");
        }
    }
}
