using System;
using System.Collections.Generic;
using System.Linq;

namespace LuC.Tree
{
    public class Function : TreeObject
    {
        public string Name { get; }
        public string ReturnType { get; }
        public Field[] Parameters { get; }
        public Field[] Locals { get; }
        public IEnumerable<Statement> Body { get; }
        public bool Native { get; private set; } = false;
        public bool Inline { get; private set; } = false;
        public DeferredEmitter NativeBody { get; private set; }

        public ulong FrameSize => Parameters.Concat(Locals).Select(f => f.Type.Size).Aggregate((a, b) => a + b);

        public Function(int start, int length, string name, string returnType, IEnumerable<Field> parameters, IEnumerable<Statement> body) : base(start, length)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
            Body = body.ToArray();
            Locals = Body.SelectMany(s => s.Locals).ToArray();

            foreach (var p in Parameters)
            {
                p.SetParent(this);
            }

            foreach (var s in Body)
            {
                s.SetParent(this);
            }
        }

        public override string ToString()
        {
            return $"{Name}({string.Join(',', Parameters.Select(p => p.ToString()))})";
        }

        public static Function CreateNativeFunction(string name, string returnType, IEnumerable<Field> parameters, Action<IEmitter, Function> emitNative, bool inline)
        {
            var body = new DeferredEmitter();

            var f = new Function(0, 0, name, returnType, parameters, new Statement[0])
            {
                Native = true,
                Inline = inline,
                NativeBody = body
            };

            emitNative(body, f);

            return f;
        }
    }
}
