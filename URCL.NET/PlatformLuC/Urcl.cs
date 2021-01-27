using System;
using System.Collections.Generic;
using System.Linq;
using LuC;
using LuC.Tree;

namespace URCL.NET.PlatformLuC.Emitters
{
    public class Urcl : IEmitter
    {
        private const ulong Zero = 0;
        private const ulong OperandA = 1;
        private const ulong OperandB = 2;
        private const ulong CallStack = 3;

        public IEnumerable<UrclInstruction> Result => Instructions;

        public ulong ValueStackSize { get; set; } = 1024;

        private readonly List<UrclInstruction> Instructions = new List<UrclInstruction>();
        private readonly Dictionary<Function, Label> FunctionLabels = new Dictionary<Function, Label>();
        private readonly Dictionary<Function, Label> ExitLabels = new Dictionary<Function, Label>();
        private readonly LuC.Compiler Compiler;

        public Urcl(LuC.Compiler compiler)
        {
            Compiler = compiler;

            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Initialize" }));

            Emit(new UrclInstruction(Operation.MOV, OperandType.Register, OperandA, OperandType.Register, Zero));
            Emit(new UrclInstruction(Operation.MOV, OperandType.Register, OperandB, OperandType.Register, Zero));
            Emit(new UrclInstruction(Operation.MOV, OperandType.Register, CallStack, OperandType.Register, Zero));
        }

        private void Emit(UrclInstruction instruction)
        {
            Instructions.Add(instruction);
        }

        private DataType ResolveType(TreeObject context, string type)
        {
            return Compiler.ResolveOneType(context, type);
        }

        private static string GetUniqueFunctionName(Function f)
        {
            return f.Parameters.Any() ? $"{DropDots(f.GetNamespace().Name)}_{DropDots(f.Name)}_{string.Join('_', f.Parameters.Select(p => p.Type.UniqueID))}" : $"{DropDots(f.GetNamespace().Name)}.{DropDots(f.Name)}";
        }

        private static string DropDots(string str)
        {
            return str.Replace('.', '_');
        }

        private struct UrclLabel : ILabel
        {
            public Label Label;

            public UrclLabel(Label label)
            {
                Label = label;
            }
        }

        public void AddInt()
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Add" }));

            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandB));
            Emit(new UrclInstruction(Operation.ADD, OperandType.Register, OperandA, OperandType.Register, OperandA, OperandType.Register, OperandB));
            Emit(new UrclInstruction(Operation.PSH, OperandType.Register, OperandA));
        }

        public void AndBoolean()
        {
            throw new NotImplementedException();
        }

        public void AndInt()
        {
            throw new NotImplementedException();
        }

        public void Branch(ILabel label)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Branch" }));

            Emit(new UrclInstruction(Operation.BRA, ((UrclLabel)label).Label));
        }

        public void BranchIfNotZero(ILabel label)
        {
            throw new NotImplementedException();
        }

        public void BranchIfZero(ILabel label)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Branch if zero" }));

            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.OR, OperandType.Register, OperandA, OperandType.Register, OperandA, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.BRZ, ((UrclLabel)label).Label));
        }

        public void BranchIfPositive(ILabel label)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Branch if positive" }));

            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.OR, OperandType.Register, OperandA, OperandType.Register, OperandA, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.BRP, ((UrclLabel)label).Label));
        }

        public void BranchIfNegative(ILabel label)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Branch if negative" }));

            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.OR, OperandType.Register, OperandA, OperandType.Register, OperandA, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.BRN, ((UrclLabel)label).Label));
        }

        public void Call()
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Call" }));

            var returnPoint = new Label();
            Emit(new UrclInstruction(Operation.COMPILER_CREATELABEL, returnPoint));
            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.INC, OperandType.Register, CallStack, OperandType.Register, CallStack));
            Emit(new UrclInstruction(Operation.STR, OperandType.Register, CallStack, returnPoint));
            Emit(new UrclInstruction(Operation.BRA, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, returnPoint));
        }

        public ILabel CreateLabel()
        {
            var label = new Label();

            Emit(new UrclInstruction(Operation.COMPILER_CREATELABEL, label));

            return new UrclLabel(label);
        }

        public void DivideInt()
        {
            throw new NotImplementedException();
        }

        public void LeftShiftInt()
        {
            throw new NotImplementedException();
        }

        public void LoadConstant(ulong value)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Load Constant" }));

            Emit(new UrclInstruction(Operation.IMM, OperandType.Register, OperandA, OperandType.Immediate, value));
            Emit(new UrclInstruction(Operation.PSH, OperandType.Register, OperandA));
        }

        public void LoadFunctionPointer(Function f)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Load Function Pointer" }));

            if (!FunctionLabels.TryGetValue(f, out Label label))
            {
                label = new Label();
                FunctionLabels.Add(f, label);
            }

            Emit(new UrclInstruction(Operation.IMM, OperandType.Register, OperandA, label));
            Emit(new UrclInstruction(Operation.PSH, OperandType.Register, OperandA));
        }

        public void LoadLocal(ulong local)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Load Local" }));

            Emit(new UrclInstruction(Operation.LOD, OperandType.Register, OperandA, OperandType.Register, CallStack));
            Emit(new UrclInstruction(Operation.SUB, OperandType.Register, OperandA, OperandType.Register, CallStack, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.ADD, OperandType.Register, OperandA, OperandType.Register, OperandA, OperandType.Immediate, local));
            Emit(new UrclInstruction(Operation.LOD, OperandType.Register, OperandB, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.PSH, OperandType.Register, OperandB));
        }

        public void MarkLabel(ILabel label)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Label" }));

            Emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, ((UrclLabel)label).Label));
        }

        public void ModuloInt()
        {
            throw new NotImplementedException();
        }

        public void MultiplyInt()
        {
            throw new NotImplementedException();
        }

        public void NotBoolean()
        {
            throw new NotImplementedException();
        }

        public void NotInt()
        {
            throw new NotImplementedException();
        }

        public void OrBoolean()
        {
            throw new NotImplementedException();
        }

        public void OrInt()
        {
            throw new NotImplementedException();
        }

        public void Return(Function f)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Return" }));

            if (!ExitLabels.TryGetValue(f, out Label label))
            {
                label = new Label();
                Emit(new UrclInstruction(Operation.COMPILER_CREATELABEL, label));
                ExitLabels.Add(f, label);
            }

            Emit(new UrclInstruction(Operation.BRA, label));
        }

        public void RightShiftInt()
        {
            throw new NotImplementedException();
        }

        public void StoreLocal(ulong local)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Store Local" }));

            Emit(new UrclInstruction(Operation.POP, OperandType.Register, OperandB));
            Emit(new UrclInstruction(Operation.LOD, OperandType.Register, OperandA, OperandType.Register, CallStack));
            Emit(new UrclInstruction(Operation.SUB, OperandType.Register, OperandA, OperandType.Register, CallStack, OperandType.Register, OperandA));
            Emit(new UrclInstruction(Operation.ADD, OperandType.Register, OperandA, OperandType.Register, OperandA, OperandType.Immediate, local));
            Emit(new UrclInstruction(Operation.STR, OperandType.Register, OperandA, OperandType.Register, OperandB));
        }

        public void SubtractInt()
        {
            throw new NotImplementedException();
        }

        public void XorBoolean()
        {
            throw new NotImplementedException();
        }

        public void XorInt()
        {
            throw new NotImplementedException();
        }

        public void BeginFunction(Function f)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Begin Function" }));

            Emit(new UrclInstruction(Operation.COMPILER_PRAGMA, new[] { "function", "begin", GetUniqueFunctionName(f), ResolveType(f, f.ReturnType).Size.ToString() }.Concat(f.Parameters.Select(p => p.Type.Size.ToString())).ToArray()));

            if (!FunctionLabels.TryGetValue(f, out Label label))
            {
                label = new Label();
                Emit(new UrclInstruction(Operation.COMPILER_CREATELABEL, label));
                FunctionLabels.Add(f, label);
            }

            Emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, label));

            if (f.FrameSize != 0)
            {
                Emit(new UrclInstruction(Operation.ADD, OperandType.Register, CallStack, OperandType.Register, CallStack, OperandType.Immediate, f.FrameSize + 1));

                Emit(new UrclInstruction(Operation.STR, OperandType.Register, CallStack, OperandType.Immediate, f.FrameSize));
            }
        }

        public void EndFunction(Function f)
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "End Function" }));

            if (!ExitLabels.TryGetValue(f, out Label label))
            {
                label = new Label();
                Emit(new UrclInstruction(Operation.COMPILER_CREATELABEL, label));
                ExitLabels.Add(f, label);
            }

            Emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, label));

            Emit(new UrclInstruction(Operation.SUB, OperandType.Register, CallStack, OperandType.Register, CallStack, OperandType.Immediate, f.FrameSize + 2));

            Emit(new UrclInstruction(Operation.INC, OperandType.Register, OperandA, OperandType.Register, CallStack));

            Emit(new UrclInstruction(Operation.LOD, OperandType.Register, OperandB, OperandType.Register, OperandA));

            Emit(new UrclInstruction(Operation.BRA, OperandType.Register, OperandB));

            Emit(new UrclInstruction(Operation.COMPILER_PRAGMA, new[] { "function", "end", GetUniqueFunctionName(f) }));
        }

        public void Pop()
        {
            Emit(new UrclInstruction(Operation.COMPILER_COMMENT, new[] { "Pop" }));

            Emit(new UrclInstruction(Operation.POP, OperandType.Register, Zero));
        }

        public void LoadGlobal(ulong global)
        {
            Emit(new UrclInstruction(Operation.PSH, OperandType.Register, CallStack + 1 + global));
        }

        public void StoreGlobal(ulong global)
        {
            Emit(new UrclInstruction(Operation.POP, OperandType.Register, CallStack + 1 + global));
        }
    }
}
