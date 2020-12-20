using System.Collections.Generic;

namespace SpeedAsm
{
    public interface ILinePreparser
    {
        /// <summary>
        /// Called at the beginning of a parsing session.
        /// </summary>
        void Begin();
        /// <summary>
        /// Called when an instruction is generated during a parsing session.
        /// </summary>
        /// <param name="inst"></param>
        void Generated(Instruction inst);
        /// <summary>
        /// Called at the end of a parsing session.
        /// </summary>
        void End();
        /// <summary>
        /// Attempt to use the preprocessor to parse the specified line.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <param name="result">The resulting instructions, if successful.</param>
        /// <returns>Whether the preparser successfully parsed the line.</returns>
        bool TryParse(string line, out IEnumerable<Instruction> result);
    }
}
