namespace URCL.NET
{
    public class Configuration
    {
        /// <summary>
        /// Enable emitting CIL NOP opcodes to URCL.
        /// </summary>
        public bool EmitNop { get; set; } = false;

        /// <summary>
        /// Enable emitting CIL BRK opcodes to URCL.
        /// </summary>
        public bool EmitBrk { get; set; } = false;

        /// <summary>
        /// The amount of memory available.
        /// </summary>
        public ulong AvailableMemory { get; set; } = ushort.MaxValue;

        /// <summary>
        /// The amount of read-only memory available.
        /// </summary>
        public ulong AvailableROM { get; set; } = 0;

        /// <summary>
        /// The size of memory words, in units of bits.
        /// </summary>
        public int WordSize { get; set; } = 8;

        /// <summary>
        /// Allow immediate values in any instruction.
        /// </summary>
        public bool EnableDirectImmediate { get; set; } = false;

        /// <summary>
        /// Allow using barrel shifting instructions.
        /// </summary>
        public bool EnableBarrelShift { get; set; } = false;

        /// <summary>
        /// Allow using push and pop instructions.
        /// </summary>
        public bool EnablePushPop { get; set; } = false;

        /// <summary>
        /// Allow use of call and return instructions. <see cref="Registers.StackPointer"/> and <see cref="Registers.BasePointer"/> must also be supported.
        /// </summary>
        public bool EnableCallRet { get; set; } = false;

        /// <summary>
        /// Enable if the stack pointer points to the next location to push to, otherwise the stack pointer should point to the currently pushed location. <see cref="Registers.StackPointer"/> and <see cref="Registers.BasePointer"/> must also be supported.
        /// </summary>
        public bool StackPointerIsNext { get; set; } = false;

        /// <summary>
        /// The bit mask defined by <see cref="WordSize"/>.
        /// </summary>
        public ulong WordBitMask => GetBitMask(WordSize);

        /// <summary>
        /// The name of the output file.
        /// </summary>
        public string Output { get; set; } = null;

        /// <summary>
        /// Emulate input using the URCL VM.
        /// </summary>
        public bool Emulate { get; set; } = false;

        /// <summary>
        /// Enable per instruction step through.
        /// </summary>
        public bool StepThrough { get; set; } = false;

        /// <summary>
        /// The code section is read-only.
        /// </summary>
        public bool ExecuteOnROM { get; set; } = false;

        /// <summary>
        /// The maximum number of registers.
        /// </summary>
        public ulong Registers { get; set; } = 16;

        public static ulong GetBitMask(int bits)
        {
            if (bits >= 64)
            {
                return ulong.MaxValue;
            }
            else if (bits <= 0)
            {
                return 0;
            }
            else
            {
                return ulong.MaxValue >> (64 - bits);
            }
        }
    }
}
