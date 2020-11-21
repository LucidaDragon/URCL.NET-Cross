using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class CallExpression : Expression
    {
        public override string ReturnType => $"{Compiler.TypeOf}({Name}({string.Join(',', ArgumentTypes)}))";

        public FunctionReference Reference => new FunctionReference(Start, Length, Name, ArgumentTypes);

        public IEnumerable<string> ArgumentTypes => Arguments.Select(arg => arg.ReturnType);

        public string Name { get; }
        public IEnumerable<Expression> Arguments { get; }

        public CallExpression(int start, int length, string name, IEnumerable<Expression> arguments) : base(start, length)
        {
            Name = name;
            Arguments = arguments;

            foreach (var arg in Arguments)
            {
                arg.SetParent(this);
            }
        }
    }
}
