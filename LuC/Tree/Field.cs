namespace LuC.Tree
{
    public class Field : TreeObject
    {
        public DataType Type { get; }
        public string Name { get; }

        public Field(int start, int length, DataType type, string name) : base(start, length)
        {
            Type = type;
            Name = name;
        }

        public Field(int start, int length, Compiler compiler, string type, string name) : base(start, length)
        {
            Type = new UnresolvedDataType(start, length, compiler, type);
            Type.SetParent(this);
            Name = name;
        }

        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
