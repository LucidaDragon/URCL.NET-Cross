using System.Collections.Generic;

namespace SpeedAsm
{
    public interface IPostProcess
    {
        /// <summary>
        /// Called at the beginning of a parsing session.
        /// </summary>
        void Begin(Parser parser);
        /// <summary>
        /// Called when an instruction is generated during a parsing session.
        /// </summary>
        /// <param name="parser">The current parser.</param>
        /// <param name="inst">The instruction that was generated.</param>
        void Generated(Parser parser, Instruction inst);
        /// <summary>
        /// Called at the end of a parsing session.
        /// </summary>
        void End(Parser parser);
        /// <summary>
        /// Post-process an instruction and return the resulting instructions.
        /// </summary>
        /// <param name="parser">The current parser.</param>
        /// <param name="inst">The instruction that was generated.</param>
        /// <returns>The resulting instructions.</returns>
        IEnumerable<Instruction> Process(Parser parser, Instruction inst);
    }
}
