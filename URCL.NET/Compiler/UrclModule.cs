using System;
using System.Collections.Generic;
using System.Linq;

namespace URCL.NET.Compiler
{
    public class UrclModule
    {
        public const string NameFileURCL = "URCL";

        public static Configuration Configuration { get; set; }

        public void HandleEmitURCL(Action<string> emit, IEnumerable<UrclInstruction> instructions)
        {
            var optimizer = new UrclOptimizer
            {
                CullCreateLabel = true,
                CullRedundantMoves = true,
                CullRedundantStackOps = true,
                ReplaceImmZeroWithZeroRegister = true,
                Compatibility = Configuration.Compatibility
            };

            foreach (var inst in optimizer.Optimize(instructions.ToArray()))
            {
                emit(inst.ToString());
            }
        }
    }
}
