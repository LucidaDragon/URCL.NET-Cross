namespace LuC.Tree
{
    public class LiteralExpression : Expression
    {
        public override string ReturnType => Type;

        public string Type { get; }
        public string Value { get; }

        public LiteralExpression(int start, int length, string type, string value) : base(start, length)
        {
            Type = type;
            Value = value;
        }
    }
}
