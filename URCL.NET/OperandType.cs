using System;

namespace URCL.NET
{
    [Flags]
    public enum OperandType
    {
        None = 0,
        Register = 1,
        Immediate = 2,
        Label = 4,
        Comment = 8,

        Value = Register | Immediate,
        Address = Immediate | Label,
        Any = Register | Immediate | Label
    }
}
