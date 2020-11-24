using System.Collections.Generic;
using System.Reflection;
using LuC.Tree;

namespace LuC
{
    public class DeferredEmitter : IEmitter
    {
        private readonly List<Task> Tasks = new List<Task>();
        private readonly List<ILabel> DeferredLabels = new List<ILabel>();
        private int InitializedLabels = 0;

        public void Commit(IEmitter emitter, bool save)
        {
            for (int i = InitializedLabels; i < DeferredLabels.Count; i++)
            {
                if (DeferredLabels[i] is null)
                {
                    DeferredLabels[i] = emitter.CreateLabel();
                }
            }

            InitializedLabels = DeferredLabels.Count;

            foreach (var task in Tasks)
            {
                for (int i = 0; i < task.Arguments.Length; i++)
                {
                    if (task.Arguments[i] is DeferredLabel l)
                    {
                        task.Arguments[i] = DeferredLabels[l.Index];
                    }
                }

                task.Method.Invoke(emitter, task.Arguments);
            }

            if (!save) Tasks.Clear();
        }

        private void Do(string name, object arg = null)
        {
            Tasks.Add(new Task(typeof(IEmitter).GetMethod(name), arg is null ? null : new[] { arg }));
        }

        private ILabel CreateDeferredLabel()
        {
            DeferredLabels.Add(null);

            return new DeferredLabel(DeferredLabels.Count - 1);
        }

        private struct Task
        {
            public MethodInfo Method;
            public object[] Arguments;
            
            public Task(MethodInfo method, object[] arguments)
            {
                Method = method;
                Arguments = arguments;
            }
        }

        private struct DeferredLabel : ILabel
        {
            public int Index;

            public DeferredLabel(int index)
            {
                Index = index;
            }
        }

        public void AddInt()
        {
            Do(nameof(AddInt));
        }

        public void AndBoolean()
        {
            Do(nameof(AndBoolean));
        }

        public void AndInt()
        {
            Do(nameof(AndInt));
        }

        public void Call()
        {
            Do(nameof(Call));
        }

        public ILabel CreateLabel()
        {
            return CreateDeferredLabel();
        }

        public void DivideInt()
        {
            Do(nameof(DivideInt));
        }

        public void LeftShiftInt()
        {
            Do(nameof(LeftShiftInt));
        }

        public void LoadConstant(ulong value)
        {
            Do(nameof(LoadConstant), value);
        }

        public void LoadFunctionPointer(Function f)
        {
            Do(nameof(LoadFunctionPointer), f);
        }

        public void LoadLocal(ulong local)
        {
            Do(nameof(LoadLocal), local);
        }

        public void MarkLabel(ILabel label)
        {
            Do(nameof(MarkLabel), label);
        }

        public void ModuloInt()
        {
            Do(nameof(ModuloInt));
        }

        public void MultiplyInt()
        {
            Do(nameof(MultiplyInt));
        }

        public void NotBoolean()
        {
            Do(nameof(NotBoolean));
        }

        public void NotInt()
        {
            Do(nameof(NotInt));
        }

        public void OrBoolean()
        {
            Do(nameof(OrBoolean));
        }

        public void OrInt()
        {
            Do(nameof(OrInt));
        }

        public void Return(Function f)
        {
            Do(nameof(Return), f);
        }

        public void RightShiftInt()
        {
            Do(nameof(RightShiftInt));
        }

        public void StoreLocal(ulong local)
        {
            Do(nameof(StoreLocal), local);
        }

        public void SubtractInt()
        {
            Do(nameof(SubtractInt));
        }

        public void XorBoolean()
        {
            Do(nameof(XorBoolean));
        }

        public void XorInt()
        {
            Do(nameof(XorInt));
        }

        public void Branch(ILabel label)
        {
            Do(nameof(Branch), label);
        }

        public void BranchIfZero(ILabel label)
        {
            Do(nameof(BranchIfZero), label);
        }

        public void BranchIfNotZero(ILabel label)
        {
            Do(nameof(BranchIfNotZero), label);
        }

        public void BranchIfPositive(ILabel label)
        {
            Do(nameof(BranchIfPositive), label);
        }

        public void BranchIfNegative(ILabel label)
        {
            Do(nameof(BranchIfNegative), label);
        }

        public void BeginFunction(Function f)
        {
            Do(nameof(BeginFunction), f);
        }

        public void EndFunction(Function f)
        {
            Do(nameof(EndFunction), f);
        }

        public void Pop()
        {
            Do(nameof(Pop));
        }

        public void LoadGlobal(ulong global)
        {
            Do(nameof(LoadGlobal), global);
        }

        public void StoreGlobal(ulong global)
        {
            Do(nameof(StoreGlobal), global);
        }
    }
}
