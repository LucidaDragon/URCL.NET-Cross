namespace LuC.Tree
{
    public class IdentifierExpression : Expression
    {
        public override string ReturnType => $"{Compiler.TypeOf}({Name})";

        public string Name { get; }

        public IdentifierExpression(int start, int length, string name) : base(start, length)
        {
            Name = name;
        }
    }
}
