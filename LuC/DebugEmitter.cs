using System;
using System.Linq;
using LuC.Tree;

namespace LuC
{
    public class DebugEmitter : IEmitter
    {
        private ulong CurrentLabel = 0;

        private struct Label : ILabel
        {
            public ulong Id;

            public Label(ulong id)
            {
                Id = id;
            }

            public override string ToString()
            {
                return $"L_0x{Id:X}";
            }
        }

        public void AddInt()
        {
            Console.WriteLine(nameof(AddInt));
        }

        public void AndBoolean()
        {
            Console.WriteLine(nameof(AndBoolean));
        }

        public void AndInt()
        {
            Console.WriteLine(nameof(AndInt));
        }

        public void Call()
        {
            Console.WriteLine(nameof(Call));
        }

        public ILabel CreateLabel()
        {
            return new Label(CurrentLabel++);
        }

        public void DivideInt()
        {
            Console.WriteLine(nameof(DivideInt));
        }

        public void LeftShiftInt()
        {
            Console.WriteLine(nameof(LeftShiftInt));
        }

        public void LoadConstant(ulong value)
        {
            Console.WriteLine(nameof(LoadConstant) + $", {value}");
        }

        public void LoadFunctionPointer(Function f)
        {
            Console.WriteLine(nameof(LoadFunctionPointer) + $", {f}");
        }

        public void LoadLocal(ulong local)
        {
            Console.WriteLine(nameof(LoadLocal) + $", [{local}]");
        }

        public void MarkLabel(ILabel label)
        {
            Console.WriteLine($"{label}:");
        }

        public void ModuloInt()
        {
            Console.WriteLine(nameof(ModuloInt));
        }

        public void MultiplyInt()
        {
            Console.WriteLine(nameof(MultiplyInt));
        }

        public void NotBoolean()
        {
            Console.WriteLine(nameof(NotBoolean));
        }

        public void NotInt()
        {
            Console.WriteLine(nameof(NotInt));
        }

        public void OrBoolean()
        {
            Console.WriteLine(nameof(OrBoolean));
        }

        public void OrInt()
        {
            Console.WriteLine(nameof(OrInt));
        }

        public void Return(Function f)
        {
            Console.WriteLine(nameof(Return));
        }

        public void RightShiftInt()
        {
            Console.WriteLine(nameof(RightShiftInt));
        }

        public void StoreLocal(ulong local)
        {
            Console.WriteLine(nameof(StoreLocal) + $", [{local}]");
        }

        public void SubtractInt()
        {
            Console.WriteLine(nameof(SubtractInt));
        }

        public void XorBoolean()
        {
            Console.WriteLine(nameof(XorBoolean));
        }

        public void XorInt()
        {
            Console.WriteLine(nameof(XorInt));
        }

        public void Branch(ILabel label)
        {
            Console.WriteLine(nameof(Branch) + $", {label}");
        }

        public void BranchIfZero(ILabel label)
        {
            Console.WriteLine(nameof(BranchIfZero) + $", {label}");
        }

        public void BranchIfNotZero(ILabel label)
        {
            Console.WriteLine(nameof(BranchIfNotZero) + $", {label}");
        }

        public void BranchIfPositive(ILabel label)
        {
            Console.WriteLine(nameof(BranchIfPositive) + $", {label}");
        }

        public void BranchIfNegative(ILabel label)
        {
            Console.WriteLine(nameof(BranchIfNegative) + $", {label}");
        }

        public void BeginFunction(Function f)
        {
            Console.WriteLine($"start {f.Name}({string.Join(',', f.Parameters.Select(p => $"{p.Type} {p.Name}"))})");
        }

        public void EndFunction(Function f)
        {
            Console.WriteLine($"end {f.Name}({string.Join(',', f.Parameters.Select(p => $"{p.Type} {p.Name}"))})");
        }

        public void Pop()
        {
            Console.WriteLine(nameof(Pop));
        }
    }
}
