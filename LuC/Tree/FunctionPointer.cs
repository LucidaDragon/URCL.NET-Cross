namespace LuC.Tree
{
    public class FunctionPointer : DataType
    {
        public override ulong Size => 1;

        public Function Function { get; }

        public FunctionPointer(Function function) : base(function.Start, function.Length)
        {
            Function = function;
        }
    }
}
