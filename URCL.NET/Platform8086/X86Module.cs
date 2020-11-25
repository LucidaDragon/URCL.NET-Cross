using System;
using System.Collections.Generic;
using System.Linq;

namespace URCL.NET.Platform8086
{
    public class X86Module
    {
        public const string NameFileAsm = "8086 Assembly";

        public void HandleEmitAsm(Action<string> emit, IEnumerable<UrclInstruction> instructions)
        {
            new X86().Generate(emit, instructions.ToArray());
        }
    }
}
