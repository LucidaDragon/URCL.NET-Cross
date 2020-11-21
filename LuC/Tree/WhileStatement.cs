using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class WhileStatement : Statement
    {
        public override IEnumerable<Field> Locals { get; }

        public Expression Condition { get; }
        public IEnumerable<Statement> Body { get; }

        public WhileStatement(int start, int length, Expression condition, IEnumerable<Statement> body) : base(start, length)
        {
            Condition = condition;
            Body = body.ToArray();

            Condition.SetParent(this);

            Locals = new Field[0];

            foreach (var s in Body)
            {
                if (s.Locals.Any())
                {
                    Locals = Locals.Concat(s.Locals);
                }

                s.SetParent(this);
            }
        }
    }
}
