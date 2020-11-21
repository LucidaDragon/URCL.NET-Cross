namespace LuC.Tree
{
    public class UnresolvedDataType : DataType
    {
        public override ulong Size => Resolve().Size;

        public string Type { get; }

        private readonly Compiler Compiler;

        public UnresolvedDataType(int start, int length, Compiler compiler, string type) : base(start, length)
        {
            Type = type;
            Compiler = compiler;
        }

        public DataType Resolve()
        {
            return Compiler.ResolveOneType(this, Type);
        }

        public override string ToString()
        {
            return Type;
        }
    }
}
