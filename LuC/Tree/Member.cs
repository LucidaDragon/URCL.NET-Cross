namespace LuC.Tree
{
    public class Member : TreeObject
    {
        public string Name { get; }

        public Member(int start, int length, string name) : base(start, length)
        {
            Name = name;
        }
    }
}
