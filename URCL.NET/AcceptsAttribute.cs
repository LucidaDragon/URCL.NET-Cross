using System;

namespace URCL.NET
{
    public class AcceptsAttribute : Attribute
    {
        public OperandType[] Types { get; set; }

        public AcceptsAttribute(params OperandType[] types)
        {
            Types = types;
        }
    }
}
