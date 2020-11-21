using System.Collections.Generic;

namespace LuC.Tree
{
    public class FunctionReference : TreeObject
    {
        public string ReturnType => $"{Compiler.TypeOf}({Name}({string.Join(',', Arguments)}))";

        public string Name { get; }
        public IEnumerable<string> Arguments { get; }

        public FunctionReference(int start, int length, string name, IEnumerable<string> arguments) : base(start, length)
        {
            Name = name;
            Arguments = arguments;
        }
    }
}
