using System.Collections.Generic;

namespace SpeedAsm
{
    public static class Compiler
    {
        /// <summary>
        /// Compile a series of SpeedAsm lines and forward the result to an emitter.
        /// </summary>
        /// <param name="emitter">The emitter to forward to.</param>
        /// <param name="lines">The lines to compile.</param>
        /// <param name="lineIndex">The line index of an error, if one occurs.</param>
        /// <param name="error">The message of an error, if one occurs.</param>
        /// <returns>True if the task completed successfully, false if an error occurred.</returns>
        public static bool Build(IEmitter emitter, IEnumerable<string> lines, out int lineIndex, out string error)
        {
            lineIndex = 0;

            try
            {
                var parser = new Parser();

                foreach (var line in lines)
                {
                    var inst = parser.Parse(line);

                    if (inst.HasValue)
                    {
                        emitter.Emit(inst.Value);
                    }

                    lineIndex++;
                }

                lineIndex = -1;
                error = null;
                return true;
            }
            catch (CompileError ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
