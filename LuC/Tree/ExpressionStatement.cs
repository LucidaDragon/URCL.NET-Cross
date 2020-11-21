using System.Collections.Generic;

namespace LuC.Tree
{
    public class ExpressionStatement : Statement
    {
        public override IEnumerable<Field> Locals => new Field[0];

        public Expression Expression { get; }

        public ExpressionStatement(int start, int length, Expression expression) : base(start, length)
        {
            Expression = expression;
            Expression.SetParent(this);
        }
    }
}
