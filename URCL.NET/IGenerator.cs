using System;

namespace URCL.NET
{
    public interface IGenerator
    {
        void Generate(Action<string> emit, UrclInstruction[] instructions);
    }
}
