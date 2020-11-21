using System;

namespace URCL.NET
{
    public class PassthroughGenerator : IGenerator
    {
        public void Generate(Action<string> emit, UrclInstruction[] instructions)
        {
            foreach (var inst in instructions)
            {
                emit(inst.ToString());
            }
        }
    }
}
