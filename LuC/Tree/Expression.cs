namespace LuC.Tree
{
    public abstract class Expression : TreeObject
    {
        public abstract string ReturnType { get; }

        public Expression(int start, int length) : base(start, length) { }
    }
}
