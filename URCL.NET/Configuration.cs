using System;

namespace URCL.NET
{
    public class Configuration
    {
        /// <summary>
        /// Enable emitting CIL NOP opcodes to URCL.
        /// </summary>
        [Config("Enable emitting CIL NOP opcodes to URCL.")]
        public bool EmitNop { get; set; } = false;

        /// <summary>
        /// Enable emitting CIL BRK opcodes to URCL.
        /// </summary>
        [Config("Enable emitting CIL BRK opcodes to URCL.")]
        public bool EmitBrk { get; set; } = false;

        /// <summary>
        /// The amount of memory available.
        /// </summary>
        [Config("The amount of memory available.")]
        public ulong AvailableMemory { get; set; } = ushort.MaxValue;

        /// <summary>
        /// The amount of read-only memory available.
        /// </summary>
        [Config("The amount of read-only memory available.")]
        public ulong AvailableROM { get; set; } = 0;

        /// <summary>
        /// The size of memory words, in units of bits.
        /// </summary>
        [Config("The size of memory words, in units of bits.")]
        public ushort WordSize { get; set; } = 32;

        /// <summary>
        /// The bit mask defined by <see cref="WordSize"/>.
        /// </summary>
        public ulong WordBitMask => GetBitMask(WordSize);

        /// <summary>
        /// The name of the output file.
        /// </summary>
        [Config("The name of the output file.")]
        public string Output { get; set; } = null;

        /// <summary>
        /// The minimum compatibility of the resulting URCL.
        /// </summary>
        [Config("The minimum compatibility of the resulting URCL.")]
        public OperationType Compatibility { get; set; } = OperationType.CustomPragma;

        /// <summary>
        /// Emulate input using the URCL VM.
        /// </summary>
        [Config("Emulate input using the URCL VM.")]
        public bool Emulate { get; set; } = false;

        /// <summary>
        /// The maximum amount of time to emulate for, in milliseconds.
        /// </summary>
        [Config("The maximum amount of time to emulate for, in milliseconds.")]
        public ulong MaxTime { get; set; } = 1000;

        /// <summary>
        /// The maximum number of items on the emulator stack.
        /// </summary>
        public ulong MaxStack { get; set; } = 1024;

        /// <summary>
        /// Disable breakpoints while emulating.
        /// </summary>
        [Config("Disable breakpoints while emulating.")]
        public bool DisableBreak { get; set; } = false;

        /// <summary>
        /// Enable per instruction step through.
        /// </summary>
        [Config("Enable per instruction step through.")]
        public bool StepThrough { get; set; } = false;

        /// <summary>
        /// The code section is read-only.
        /// </summary>
        [Config("The code section is read-only.")]
        public bool ExecuteOnROM { get; set; } = false;

        /// <summary>
        /// The maximum number of registers.
        /// </summary>
        [Config("The maximum number of registers.")]
        public ulong Registers { get; set; } = 16;

        /// <summary>
        /// The directory to load modules from.
        /// </summary>
        [Config("The directory to load modules from.")]
        public string Modules { get; set; }

        /// <summary>
        /// Open a port to allow other applications control over the compiler.
        /// </summary>
        [Config("Open a port to allow other applications control over the compiler.")]
        public ushort ApiPort { get; set; }

        /// <summary>
        /// Show configuration help.
        /// </summary>
        [Config("Show configuration help.")]
        public bool Help
        {
            get => false;
            set
            {
                foreach (var prop in GetType().GetProperties())
                {
                    var attribs = prop.GetCustomAttributes(typeof(ConfigAttribute), false);

                    if (attribs.Length > 0)
                    {
                        var attrib = (ConfigAttribute)attribs[0];

                        Console.WriteLine($"{$"{prop.Name} = {prop.GetValue(this)}",-32} - {attrib.Description}");
                    }
                }
            }
        }

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

        [AttributeUsage(AttributeTargets.Property)]
        private class ConfigAttribute : Attribute
        {
            public readonly string Description;

            public ConfigAttribute(string desc)
            {
                Description = desc;
            }
        }
    }
}
