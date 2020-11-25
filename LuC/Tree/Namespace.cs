using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class Namespace : TreeObject
    {
        public string Name { get; }

        public IEnumerable<Member> Members { get; private set; }

        public IEnumerable<Function> Functions => Members.Where(m => m is Function).Select(f => (Function)f);
        public IEnumerable<Structure> Structures => Members.Where(m => m is Structure).Select(s => (Structure)s);

        public Namespace(int start, int length, string name, IEnumerable<Member> members) : base(start, length)
        {
            Name = name;
            Members = members.ToArray();

            foreach (var m in Members)
            {
                m.SetParent(this);
            }
        }

        public void Merge(Namespace ns)
        {
            if (ns.Name != Name) new SourceError(ns.Start, ns.Length, $"Different namespaces \"{ns.Name}\" and \"{Name}\" can not be merged.");

            Members = Members.Concat(ns.Members);
        }
    }
}
