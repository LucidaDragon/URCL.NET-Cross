namespace LuC.Tree
{
    public class VoidExpression : Expression
    {
        public override string ReturnType => Compiler.Void;

        public VoidExpression(int start, int length) : base(start, length) { }
    }
}
