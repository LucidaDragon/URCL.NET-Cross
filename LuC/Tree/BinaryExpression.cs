namespace LuC.Tree
{
    public class BinaryExpression : Expression
    {
        public override string ReturnType => Operator.ReturnType;

        public Expression A { get; }
        public FunctionReference Operator { get; }
        public Expression B { get; }

        public BinaryExpression(int start, int length, Expression a, FunctionReference op, Expression b) : base(start, length)
        {
            A = a;
            Operator = op;
            B = b;

            A.SetParent(this);
            B.SetParent(this);
            Operator.SetParent(this);
        }
    }
}
