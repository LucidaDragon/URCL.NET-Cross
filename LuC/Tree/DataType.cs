namespace LuC.Tree
{
    public abstract class DataType : TreeObject
    {
        private static ulong NextId = 0;

        public abstract ulong Size { get; }

        public string UniqueID => Id.ToString("X");

        private readonly ulong Id;

        public DataType(int start, int length) : base(start, length) 
        {
            Id = NextId++;
        }

        public bool Equals(DataType type)
        {
            return Id == type.Id;
        }
    }
}
