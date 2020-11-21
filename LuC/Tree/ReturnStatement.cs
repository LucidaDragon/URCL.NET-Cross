using System.Collections.Generic;

namespace LuC.Tree
{
    public class ReturnStatement : Statement
    {
        public override IEnumerable<Field> Locals => new Field[0];

        public Expression Result { get; }

        public ReturnStatement(int start, int length, Expression result = null) : base(start, length)
        {
            Result = result;

            Result.SetParent(this);
        }
    }
}
