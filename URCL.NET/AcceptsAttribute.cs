using System;

namespace URCL.NET
{
    public class AcceptsAttribute : Attribute
    {
        public OperationType Type { get; set; }
        public OperandType[] Types { get; set; }

        public AcceptsAttribute(OperationType type, params OperandType[] types)
        {
            Type = type;
            Types = types;
        }
    }
}
