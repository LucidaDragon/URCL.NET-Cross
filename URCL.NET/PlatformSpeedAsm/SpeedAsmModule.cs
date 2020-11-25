using System;
using System.Collections.Generic;

namespace URCL.NET.PlatformSpeedAsm
{
    public class SpeedAsmModule
    {
        public const string NameFileSped = "Speed Assembly";

        private readonly SpeedAsm Wrapper = new SpeedAsm();

        public void HandleFileSped(Action<UrclInstruction> emit, IEnumerable<string> src)
        {
            if (!global::SpeedAsm.Compiler.Build(Wrapper, src, out int line, out string error))
            {
                Console.WriteLine($"Compile Error: Ln {line + 1}, {error}");
                Environment.Exit(NameFileSped.GetHashCode());
            }

            while (Wrapper.Instructions.Count > 0)
            {
                emit(Wrapper.Instructions.Dequeue());
            }
        }
    }
}
