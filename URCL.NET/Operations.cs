using System.Collections.Generic;

namespace URCL.NET
{
    public enum Operation
    {
        [Accepts(OperationType.Core)]
        NOP = 0,
        [Accepts(OperationType.Custom)]
        BRK,
        [Accepts(OperationType.Core)]
        HLT,

        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        ADD,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any)]
        INC,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        SUB,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any)]
        DEC,
        [Accepts(OperationType.Complex, OperandType.Register, OperandType.Any, OperandType.Any)]
        MLT,
        [Accepts(OperationType.Complex, OperandType.Register, OperandType.Any, OperandType.Any)]
        DIV,
        [Accepts(OperationType.Complex, OperandType.Register, OperandType.Any, OperandType.Any)]
        MOD,
        [Accepts(OperationType.Complex, OperandType.Any, OperandType.Any)]
        CMP,

        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        AND,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        OR,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        XOR,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        NAND,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        NOR,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any, OperandType.Any)]
        XNOR,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any)]
        NOT,

        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any)]
        LSH,
        [Accepts(OperationType.Complex, OperandType.Register, OperandType.Any, OperandType.Any)]
        BSL,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any)]
        RSH,
        [Accepts(OperationType.Complex, OperandType.Register, OperandType.Any, OperandType.Any)]
        BSR,

        [Accepts(OperationType.Core, OperandType.Register, OperandType.Register)]
        MOV,
        [Accepts(OperationType.Core, OperandType.Register, OperandType.Address)]
        IMM,

        [Accepts(OperationType.Core, OperandType.Register, OperandType.Any)]
        LOAD,
        [Accepts(OperationType.Core, OperandType.Any, OperandType.Register)]
        STORE,

        [Accepts(OperationType.Basic, OperandType.Register, OperandType.Immediate)]
        IN,
        [Accepts(OperationType.Basic, OperandType.Immediate, OperandType.Any)]
        OUT,

        [Accepts(OperationType.Basic, OperandType.Any)]
        PSH,
        [Accepts(OperationType.Basic, OperandType.Register)]
        POP,

        [Accepts(OperationType.Core, OperandType.Any)]
        BRA,
        [Accepts(OperationType.Core, OperandType.Any)]
        BRZ,
        [Accepts(OperationType.Core, OperandType.Any)]
        BNZ,
        [Accepts(OperationType.Core, OperandType.Any)]
        BRC,
        [Accepts(OperationType.Core, OperandType.Any)]
        BNC,
        [Accepts(OperationType.Core, OperandType.Any)]
        BRP,
        [Accepts(OperationType.Core, OperandType.Any)]
        BRN,
        
        [Accepts(OperationType.Complex, OperandType.Any, OperandType.Value)]
        BEV,
        [Accepts(OperationType.Complex, OperandType.Any, OperandType.Value)]
        BOD,

        [Accepts(OperationType.Complex, OperandType.Any)]
        CAL,
        [Accepts(OperationType.Complex)]
        RET,

        [Accepts(OperationType.Core, OperandType.Immediate)]
        DW,
        [Accepts(OperationType.Core, OperandType.Immediate)]
        DD,
        [Accepts(OperationType.Core, OperandType.Immediate)]
        DQ,

        [Accepts(OperationType.Pragma, OperandType.Immediate)]
        MINRAM,
        [Accepts(OperationType.Pragma, OperandType.Immediate)]
        BITS,
        [Accepts(OperationType.Pragma, OperandType.String)]
        IMPORT,

        [Accepts(OperationType.CustomPragma)]
        BENCHMARK,

        COMPILER_CREATELABEL,
        COMPILER_MARKLABEL,
        COMPILER_PRAGMA,
        COMPILER_VALUEMACRO,
        COMPILER_CODEMACRO_BEGIN,
        COMPILER_CODEMACRO_END,
        COMPILER_CODEMACRO_USE,
        COMPILER_MAXREG,
        COMPILER_COMMENT
    }

    public static class OperationExtensions
    {
        private static readonly Dictionary<Operation, AcceptsAttribute> Attributes = new Dictionary<Operation, AcceptsAttribute>();
        
        static OperationExtensions()
        {
            foreach (var field in typeof(Operation).GetFields())
            {
                var attribs = field.GetCustomAttributes(typeof(AcceptsAttribute), false);

                if (field.IsStatic && attribs.Length == 1 && field.FieldType == typeof(Operation))
                {
                    var attrib = (AcceptsAttribute)attribs[0];
                    var value = (Operation)field.GetValue(null);

                    Attributes[value] = attrib;
                }
            }
        }

        public static AcceptsAttribute GetAttributes(this Operation op)
        {
            if (Attributes.TryGetValue(op, out AcceptsAttribute result))
            {
                return result;
            }
            else
            {
                return new AcceptsAttribute(OperationType.Pragma);
            }
        }
    }
}
