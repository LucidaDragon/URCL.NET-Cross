using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class IfStatement : Statement
    {
        public override IEnumerable<Field> Locals { get; }

        public Expression Condition { get; }
        public IEnumerable<Statement> TrueBody { get; }
        public IEnumerable<Statement> FalseBody { get; }

        public IfStatement(int start, int length, Expression condition, IEnumerable<Statement> trueBody, IEnumerable<Statement> falseBody) : base(start, length)
        {
            Condition = condition;
            Condition.SetParent(this);

            Locals = new Field[0];

            TrueBody = trueBody.ToArray();
            foreach (var s in TrueBody)
            {
                if (s.Locals.Any())
                {
                    Locals = Locals.Concat(s.Locals);
                }

                s.SetParent(this);
            }

            FalseBody = falseBody.ToArray();
            foreach (var s in FalseBody)
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
