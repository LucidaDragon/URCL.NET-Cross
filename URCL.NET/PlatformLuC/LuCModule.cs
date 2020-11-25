using System;
using System.Collections.Generic;
using System.Linq;

namespace URCL.NET.PlatformLuC
{
    public class LuCModule
    {
        public const string NameFileC = "LuC";
        public const string NameFileH = "LuC";
        public const string NameFileL = "LuC";

        public void HandleFileC(Action<UrclInstruction> emit, IEnumerable<string> src)
        {
            HandleFileL(emit, src);
        }

        public void HandleFileH(Action<UrclInstruction> emit, IEnumerable<string> src)
        {
            HandleFileL(emit, src);
        }

        public void HandleFileL(Action<UrclInstruction> emit, IEnumerable<string> src)
        {
            var compiler = new LuC.Compiler();
            
            compiler.Compile(LuC.Parser.Parse(compiler, string.Join(Environment.NewLine, src)));
            
            LuC.Standard.AddStandardDefaults(compiler);

            var emitter = new Emitters.Urcl(compiler);
            compiler.Emit(emitter, "Program.Main");

            foreach (var inst in new UrclOptimizer
            {
                CullRedundantStackOps = true,
                CullCreateLabel = true,
                CullPragmas = true
            }.Optimize(emitter.Result.ToArray()))
            {
                emit(inst);
            }
        }
    }
}
