namespace LuC.Tree
{
    public abstract class DataType : Member
    {
        private static ulong NextId = 0;

        public abstract ulong Size { get; }

        public string UniqueID => Id.ToString("X");

        private readonly ulong Id;

        public DataType(int start, int length, string name) : base(start, length, name)
        {
            Id = NextId++;
        }

        public bool Equals(DataType type)
        {
            return Id == type.Id;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
