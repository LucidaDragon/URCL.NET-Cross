using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class Namespace : TreeObject
    {
        public string Name { get; }

        public IEnumerable<Function> Functions { get; private set; }

        public Namespace(int start, int length, string name, IEnumerable<Function> functions) : base(start, length)
        {
            Name = name;
            Functions = functions.ToArray();

            foreach (var f in Functions)
            {
                f.SetParent(this);
            }
        }

        public void Merge(Namespace ns)
        {
            if (ns.Name != Name) new SourceError(ns.Start, ns.Length, $"Different namespaces \"{ns.Name}\" and \"{Name}\" can not be merged.");

            Functions = Functions.Concat(ns.Functions);
        }
    }
}
