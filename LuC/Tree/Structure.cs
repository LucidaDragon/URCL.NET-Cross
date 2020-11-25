using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class Structure : DataType
    {
        public override ulong Size => Fields.Select(f => f.Type.Size).Aggregate((a, b) => a + b);

        public Field[] Fields { get; }

        public Structure(int start, int length, string name, IEnumerable<Field> fields) : base(start, length, name)
        {
            Fields = fields.ToArray();

            foreach (var field in Fields)
            {
                field.SetParent(this);
            }
        }
    }
}
