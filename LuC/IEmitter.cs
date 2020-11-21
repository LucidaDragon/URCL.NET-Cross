using LuC.Tree;

namespace LuC
{
    public interface IEmitter
    {
        ILabel CreateLabel();

        void MarkLabel(ILabel label);

        void BeginFunction(Function f);

        void EndFunction(Function f);

        void LoadConstant(ulong value);

        void LoadFunctionPointer(Function f);

        void LoadLocal(ulong local);

        void StoreLocal(ulong local);

        void Branch(ILabel label);

        void BranchIfZero(ILabel label);

        void BranchIfNotZero(ILabel label);

        void BranchIfPositive(ILabel label);

        void BranchIfNegative(ILabel label);

        void Call();

        void Return(Function f);

        void AddInt();

        void SubtractInt();

        void MultiplyInt();

        void DivideInt();

        void ModuloInt();

        void OrInt();

        void AndInt();

        void XorInt();

        void NotInt();

        void LeftShiftInt();

        void RightShiftInt();

        void OrBoolean();

        void AndBoolean();

        void XorBoolean();

        void NotBoolean();

        void Pop();
    }
}
