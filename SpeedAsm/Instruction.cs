namespace SpeedAsm
{
    public struct Instruction
    {
        public Operation Operation;
        public Operand Destination;
        public Operand Source;
        public Operand Target;

        public Instruction(Operation op)
        {
            Operation = op;
            Destination = default;
            Source = default;
            Target = default;
        }

        public Instruction(Operation op, Operand destination) : this(op)
        {
            Destination = destination;
            Source = default;
            Target = default;

            if (destination.Immediate && !destination.Label) throw new CompileError(CompileError.ImmediateAsVariable);
        }

        public Instruction(Operation op, Operand destination, Operand source) : this(op, destination)
        {
            Source = source;
            Target = default;
        }

        public Instruction(Operation op, Operand destination, Operand source, Operand target) : this(op, destination, source)
        {
            Target = target;
        }
    }

    public struct Operand
    {
        public bool Immediate;
        public bool Label;
        public ulong Value;

        public Operand(ulong value, bool immediate, bool label)
        {
            Value = value;
            Immediate = immediate;
            Label = label;
        }
    }
}
