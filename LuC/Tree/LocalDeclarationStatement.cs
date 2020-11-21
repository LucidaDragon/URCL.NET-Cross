using System.Collections.Generic;

namespace LuC.Tree
{
    public class LocalDeclarationStatement : Statement
    {
        public override IEnumerable<Field> Locals => new[] { Local };

        public Field Local { get; }

        public LocalDeclarationStatement(int start, int length, Field local) : base(start, length)
        {
            Local = local;

            Local.SetParent(this);
        }
    }
}
