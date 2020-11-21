using System.Collections.Generic;

namespace LuC.Tree
{
    public abstract class Statement : TreeObject
    {
        public abstract IEnumerable<Field> Locals { get; }

        public Statement(int start, int length) : base(start, length) { }
    }
}
