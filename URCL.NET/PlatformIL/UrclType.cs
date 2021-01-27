using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace URCL.NET.PlatformIL
{
    public class UrclType
    {
        private readonly string Name;
        private readonly FieldDefinition[] Fields;
        private readonly MethodDefinition[] Methods;
        private readonly Dictionary<MethodDefinition, Label> Labels = new Dictionary<MethodDefinition, Label>();

        public UrclType(TypeDefinition type)
        {
            Name = type.Name;
            Fields = type.Fields.ToArray();
            Methods = type.Methods.ToArray();

            foreach (var method in Methods)
            {
                Labels.Add(method, new Label());
            }
        }

        public void Emit(Action<UrclInstruction> emit, StdLib std, Configuration configuration)
        {
            emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { Name }));

            foreach (var method in Methods)
            {
                EmitMethod(method, std, (m) => Labels[m], (inst) =>
                {
                    switch (inst.Operation)
                    {
                        case Operation.NOP:
                            if (configuration.EmitNop)
                            {
                                emit(inst);
                            }
                            break;
                        case Operation.BRK:
                            if (configuration.EmitBrk)
                            {
                                emit(inst);
                            }
                            break;
                        default:
                            emit(inst);
                            break;
                    }
                });
            }
        }

        private void EmitMethod(MethodDefinition method, StdLib std, Func<MethodDefinition, Label> resolve, Action<UrclInstruction> emit)
        {
            const ulong zero = 0;
            const ulong source = 1;
            const ulong operands = 3;
            var arguments = (ulong)method.Parameters.Count;
            var locals = (ulong)method.Body.Variables.Count;

            var vars = method.Body.Variables.ToArray();

            static ulong operand(ulong i) => i + 2;
            static ulong argument(ulong i) => (i + operands) + 2;
            ulong local(ulong i) => (i + operands + arguments) + 2;
            ulong passing(ulong i) => (i + operands + arguments + locals) + 2;
            
            var offsets = new Dictionary<int, int>();
            var lines = method.Body.Instructions.Select((i) => new Label()).ToArray();
            var line = 0;

            foreach (var inst in method.Body.Instructions)
            {
                offsets.Add(inst.Offset, line);
                line++;
            }

            line = 0;

            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, resolve(method)));

            foreach (var inst in method.Body.Instructions)
            {
                try
                {
                    emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { $"0x{inst.Offset:X} {inst.OpCode.Code} {(inst.Operand is null ? string.Empty : inst.Operand.ToString())}" }));

                    emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, lines[line]));

                    switch (inst.OpCode.Code)
                    {
                        case Code.Nop:
                            emit(new UrclInstruction(Operation.NOP));
                            break;
                        case Code.Break:
                            emit(new UrclInstruction(Operation.BRK));
                            break;
                        case Code.Ldarg_0:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument(0)));
                            break;
                        case Code.Ldarg_1:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument(1)));
                            break;
                        case Code.Ldarg_2:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument(2)));
                            break;
                        case Code.Ldarg_3:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument(3)));
                            break;
                        case Code.Ldloc_0:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, local(0)));
                            break;
                        case Code.Ldloc_1:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, local(1)));
                            break;
                        case Code.Ldloc_2:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, local(2)));
                            break;
                        case Code.Ldloc_3:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, local(3)));
                            break;
                        case Code.Stloc_0:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, local(0)));
                            break;
                        case Code.Stloc_1:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, local(1)));
                            break;
                        case Code.Stloc_2:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, local(2)));
                            break;
                        case Code.Stloc_3:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, local(3)));
                            break;
                        case Code.Ldarg_S:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument((byte)inst.Operand)));
                            break;
                        case Code.Ldarga_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Starg_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, argument((byte)inst.Operand)));
                            break;
                        case Code.Ldloc_S:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, local((ulong)Array.IndexOf(vars, (VariableDefinition)inst.Operand))));
                            break;
                        case Code.Ldloca_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stloc_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, local((ulong)Array.IndexOf(vars, (VariableDefinition)inst.Operand))));
                            break;
                        case Code.Ldnull:
                            Push(emit, zero);
                            break;
                        case Code.Ldc_I4_M1:
                            Push(emit, ulong.MaxValue);
                            break;
                        case Code.Ldc_I4_0:
                            Push(emit, zero);
                            break;
                        case Code.Ldc_I4_1:
                            Push(emit, 1);
                            break;
                        case Code.Ldc_I4_2:
                            Push(emit, 2);
                            break;
                        case Code.Ldc_I4_3:
                            Push(emit, 3);
                            break;
                        case Code.Ldc_I4_4:
                            Push(emit, 4);
                            break;
                        case Code.Ldc_I4_5:
                            Push(emit, 5);
                            break;
                        case Code.Ldc_I4_6:
                            Push(emit, 6);
                            break;
                        case Code.Ldc_I4_7:
                            Push(emit, 7);
                            break;
                        case Code.Ldc_I4_8:
                            Push(emit, 8);
                            break;
                        case Code.Ldc_I4_S:
                            Push(emit, (ulong)(sbyte)inst.Operand);
                            break;
                        case Code.Ldc_I4:
                            Push(emit, (ulong)(int)inst.Operand);
                            break;
                        case Code.Ldc_I8:
                            Push(emit, (ulong)(long)inst.Operand);
                            break;
                        case Code.Ldc_R4:
                            Push(emit, (ulong)(float)inst.Operand);
                            break;
                        case Code.Ldc_R8:
                            Push(emit, (ulong)(double)inst.Operand);
                            break;
                        case Code.Dup:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Pop:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, zero));
                            break;
                        case Code.Jmp:
                            emit(new UrclInstruction(Operation.BRA, resolve((MethodDefinition)inst.Operand)));
                            break;
                        case Code.Call:
                            {
                                var target = (MethodDefinition)inst.Operand;

                                emit(new UrclInstruction(Operation.PSH, OperandType.Register, source));

                                for (ulong i = (ulong)target.Parameters.Count; i > 0; i--)
                                {
                                    emit(new UrclInstruction(Operation.POP, OperandType.Register, passing(i - 1)));
                                }

                                for (ulong i = 0; i < arguments; i++)
                                {
                                    emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument(i)));
                                }

                                for (ulong i = 0; i < locals; i++)
                                {
                                    emit(new UrclInstruction(Operation.PSH, OperandType.Register, local(i)));
                                }

                                for (ulong i = 0; i < (ulong)target.Parameters.Count; i++)
                                {
                                    emit(new UrclInstruction(Operation.MOV, OperandType.Register, argument(i), OperandType.Register, passing(i)));
                                }

                                var returnPoint = new Label();

                                emit(new UrclInstruction(Operation.IMM, OperandType.Register, source, returnPoint));

                                emit(new UrclInstruction(Operation.BRA, resolve((MethodDefinition)inst.Operand)));

                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, returnPoint));

                                for (ulong i = locals; i > 0; i--)
                                {
                                    emit(new UrclInstruction(Operation.POP, OperandType.Register, local(i - 1)));
                                }

                                for (ulong i = arguments; i > 0; i--)
                                {
                                    emit(new UrclInstruction(Operation.POP, OperandType.Register, argument(i - 1)));
                                }

                                emit(new UrclInstruction(Operation.POP, OperandType.Register, source));
                            }
                            break;
                        case Code.Calli:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ret:
                            emit(new UrclInstruction(Operation.BRA, OperandType.Register, source));
                            break;
                        case Code.Br_S:
                            Jump(emit, offsets, Operation.BRA, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Brfalse_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0));
                            Jump(emit, offsets, Operation.BRZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Brtrue_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0));
                            Jump(emit, offsets, Operation.BNZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Beq_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bge_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRP, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bgt_S:
                            {
                                var skip = new Label();
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                                Flags(emit, operand(0), operand(1));
                                emit(new UrclInstruction(Operation.BRZ, skip));
                                Jump(emit, offsets, Operation.BRP, ((Instruction)inst.Operand).Offset, lines);
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, skip));
                            }
                            break;
                        case Code.Ble_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRZ, ((Instruction)inst.Operand).Offset, lines);
                            Jump(emit, offsets, Operation.BRN, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Blt_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRN, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bne_Un_S:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BNZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bge_Un_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Bgt_Un_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ble_Un_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Blt_Un_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Br:
                            Jump(emit, offsets, Operation.BRA, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Brfalse:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0));
                            Jump(emit, offsets, Operation.BRZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Brtrue:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0));
                            Jump(emit, offsets, Operation.BNZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Beq:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bge:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRP, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bgt:
                            {
                                var skip = new Label();
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                                Flags(emit, operand(0), operand(1));
                                emit(new UrclInstruction(Operation.BRZ, skip));
                                Jump(emit, offsets, Operation.BRP, ((Instruction)inst.Operand).Offset, lines);
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, skip));
                            }
                            break;
                        case Code.Ble:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRZ, ((Instruction)inst.Operand).Offset, lines);
                            Jump(emit, offsets, Operation.BRN, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Blt:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BRN, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bne_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            Flags(emit, operand(0), operand(1));
                            Jump(emit, offsets, Operation.BNZ, ((Instruction)inst.Operand).Offset, lines);
                            break;
                        case Code.Bge_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Bgt_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ble_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Blt_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Switch:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_I1:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_U1:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_I2:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_U2:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_I4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_U4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_I8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_I:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_R4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_R8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldind_Ref:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_Ref:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_I1:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_I2:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_I4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_I8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_R4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_R8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Add:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Sub:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.SUB, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Mul:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.MLT, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Div:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Div_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.DIV, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Rem:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Rem_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.MOD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.And:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Or:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.OR, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Xor:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.XOR, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Shl:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.BSL, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Shr:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.BSR, OperandType.Register, operand(1), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.RSH, OperandType.Register, operand(2), OperandType.Immediate, ulong.MaxValue));
                            emit(new UrclInstruction(Operation.NOT, OperandType.Register, operand(2), OperandType.Register, operand(2)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(2)));
                            emit(new UrclInstruction(Operation.OR, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Shr_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.BSR, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Neg:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.NOT, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Not:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.NOT, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_I1:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_I2:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_I4:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_I8:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_R4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_R8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_U4:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_U8:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Callvirt:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Cpobj:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldobj:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldstr:
                            {
                                var end = new Label();
                                var str = new Label();
                                var data = (string)inst.Operand;

                                if (data is null)
                                {
                                    emit(new UrclInstruction(Operation.PSH, OperandType.Register, zero));
                                }
                                else
                                {
                                    emit(new UrclInstruction(Operation.BRA, end));

                                    emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, str));

                                    foreach (var c in data)
                                    {
                                        emit(new UrclInstruction(Operation.DW, OperandType.Immediate, c));
                                    }

                                    emit(new UrclInstruction(Operation.DW, OperandType.Immediate, 0));

                                    emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));

                                    emit(new UrclInstruction(Operation.PSH, str));
                                }
                            }
                            break;
                        case Code.Newobj:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Castclass:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Isinst:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_R_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Unbox:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Throw:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldfld:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldflda:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stfld:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldsfld:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldsflda:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stsfld:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stobj:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_Ovf_I1_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I2_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I4_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I8_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U1_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U2_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U4_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U8_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I_Un:
                            break;
                        case Code.Conv_Ovf_U_Un:
                            break;
                        case Code.Box:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Newarr:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            std.CallStd(emit, std.Malloc);
                            emit(new UrclInstruction(Operation.OR, OperandType.Register, zero, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.BRZ, std.OutOfMemory));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.STR, OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldlen:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelema:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_I1:
                        case Code.Ldelem_U1:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_I2:
                        case Code.Ldelem_U2:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_I4:
                        case Code.Ldelem_U4:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_I8:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_I:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_R4:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_R8:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Ldelem_Ref:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.INC, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.LOD, OperandType.Register, operand(0), OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Stelem_I:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_I1:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_I2:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_I4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_I8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_R4:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_R8:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_Ref:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldelem_Any:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stelem_Any:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Unbox_Any:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_Ovf_I1:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U1:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I2:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U2:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I4:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U4:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_I8:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_Ovf_U8:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFFFFFFFFFFFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Refanyval:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ckfinite:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Mkrefany:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldtoken:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_U2:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFFFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_U1:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.AND, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Immediate, 0xFF));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Conv_I:
                            break;
                        case Code.Conv_Ovf_I:
                            break;
                        case Code.Conv_Ovf_U:
                            break;
                        case Code.Add_Ovf:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Add_Ovf_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.ADD, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Mul_Ovf:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.MLT, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Mul_Ovf_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.MLT, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Sub_Ovf:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.SUB, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Sub_Ovf_Un:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                            emit(new UrclInstruction(Operation.SUB, OperandType.Register, operand(0), OperandType.Register, operand(0), OperandType.Register, operand(1)));
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, operand(0)));
                            break;
                        case Code.Endfinally:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Leave:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Leave_S:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stind_I:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Conv_U:
                            break;
                        case Code.Arglist:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ceq:
                            {
                                var skip = new Label();
                                var end = new Label();
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                                Flags(emit, operand(0), operand(1));
                                emit(new UrclInstruction(Operation.BNZ, skip));
                                Push(emit, 1);
                                emit(new UrclInstruction(Operation.BRA, end));
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, skip));
                                Push(emit, 0);
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));
                            }
                            break;
                        case Code.Cgt:
                            {
                                var skip = new Label();
                                var end = new Label();
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                                Flags(emit, operand(0), operand(1));
                                emit(new UrclInstruction(Operation.BRN, skip));
                                emit(new UrclInstruction(Operation.BRZ, skip));
                                Push(emit, 1);
                                emit(new UrclInstruction(Operation.BRA, end));
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, skip));
                                Push(emit, 0);
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));
                            }
                            break;
                        case Code.Cgt_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Clt:
                            {
                                var skip = new Label();
                                var end = new Label();
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(1)));
                                emit(new UrclInstruction(Operation.POP, OperandType.Register, operand(0)));
                                Flags(emit, operand(0), operand(1));
                                emit(new UrclInstruction(Operation.BRN, skip));
                                Push(emit, 0);
                                emit(new UrclInstruction(Operation.BRA, end));
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, skip));
                                Push(emit, 1);
                                emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, end));
                            }
                            break;
                        case Code.Clt_Un:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldftn:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldvirtftn:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Ldarg:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, argument((ushort)inst.Operand)));
                            break;
                        case Code.Ldarga:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Starg:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, argument((ushort)inst.Operand)));
                            break;
                        case Code.Ldloc:
                            emit(new UrclInstruction(Operation.PSH, OperandType.Register, local((ulong)Array.IndexOf(vars, (VariableDefinition)inst.Operand))));
                            break;
                        case Code.Ldloca:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Stloc:
                            emit(new UrclInstruction(Operation.POP, OperandType.Register, local((ulong)Array.IndexOf(vars, (VariableDefinition)inst.Operand))));
                            break;
                        case Code.Localloc:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Endfilter:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Unaligned:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Volatile:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Tail:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Initobj:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Constrained:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Cpblk:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Initblk:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.No:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Rethrow:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Sizeof:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Refanytype:
                            Unsupported(inst.OpCode.Code);
                            break;
                        case Code.Readonly:
                            Unsupported(inst.OpCode.Code);
                            break;
                        default:
                            Unsupported(inst.OpCode.Code);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error in {inst.OpCode.Code} (line {line + 1}): {ex.Message}");
                }

                line++;
            }
        }

        private static void Push(Action<UrclInstruction> emit, ulong value)
        {
            emit(new UrclInstruction(Operation.PSH, OperandType.Immediate, value));
        }

        private static void Jump(Action<UrclInstruction> emit, Dictionary<int, int> offsets, Operation type, int offset, Label[] labels)
        {
            if (offsets.TryGetValue(offset, out int target) && target >= 0 && target < labels.Length)
            {
                emit(new UrclInstruction(type, labels[target]));
            }
            else
            {
                throw new InvalidProgramException("Branch is out of range.");
            }
        }

        private static void Flags(Action<UrclInstruction> emit, ulong operand)
        {
            emit(new UrclInstruction(Operation.OR, OperandType.Register, 0, OperandType.Register, operand, OperandType.Register, operand));
        }

        private static void Flags(Action<UrclInstruction> emit, ulong operandA, ulong operandB)
        {
            emit(new UrclInstruction(Operation.SUB, OperandType.Register, 0, OperandType.Register, operandA, OperandType.Register, operandB));
        }

        private static void Unsupported(Code op)
        {
            throw new InvalidProgramException($"Unsupported opcode {op}.");
        }
    }
}
