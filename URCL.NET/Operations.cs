namespace URCL.NET
{
    public enum Operation
    {
        [Accepts()]
        NOP = 0,
        [Accepts()]
        BRK,
        [Accepts()]
        HLT,

        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        ADD,
        [Accepts(OperandType.Register, OperandType.Value)]
        INC,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        SUB,
        [Accepts(OperandType.Register, OperandType.Value)]
        DEC,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        MLT,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        DIV,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        MOD,

        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        AND,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        OR,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        XOR,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        NAND,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        NOR,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        XNOR,
        [Accepts(OperandType.Register, OperandType.Value)]
        NOT,

        [Accepts(OperandType.Register, OperandType.Value)]
        LSH,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        BSL,
        [Accepts(OperandType.Register, OperandType.Value)]
        RSH,
        [Accepts(OperandType.Register, OperandType.Value, OperandType.Value)]
        BSR,

        [Accepts(OperandType.Register, OperandType.Register)]
        MOV,
        [Accepts(OperandType.Register, OperandType.Immediate)]
        IMM,

        [Accepts(OperandType.Register, OperandType.Value)]
        LOAD,
        [Accepts(OperandType.Value, OperandType.Register)]
        STORE,

        [Accepts(OperandType.Register, OperandType.Immediate)]
        IN,
        [Accepts(OperandType.Immediate, OperandType.Value)]
        OUT,

        [Accepts(OperandType.Value)]
        PSH,
        [Accepts(OperandType.Register)]
        POP,

        [Accepts(OperandType.Label)]
        BRA,
        [Accepts(OperandType.Label)]
        BRZ,
        [Accepts(OperandType.Label)]
        BNZ,
        [Accepts(OperandType.Label)]
        BRC,
        [Accepts(OperandType.Label)]
        BNC,
        [Accepts(OperandType.Label)]
        BRP,
        [Accepts(OperandType.Label)]
        BRN,

        [Accepts(OperandType.Label)]
        CAL,
        [Accepts()]
        RET,

        [Accepts(OperandType.Immediate)]
        MINRAM,
        [Accepts(OperandType.Immediate)]
        BITS,

        [Accepts()]
        BENCHMARK,

        COMPILER_CREATELABEL,
        COMPILER_MARKLABEL,
        COMPILER_PRAGMA,
        COMPILER_MAXREG,
        COMPILER_COMMENT
    }
}
