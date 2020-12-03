using System;

namespace URCL.NET
{
    public class UrclInstruction
    {
        public Operation Operation { get; set; }

        public OperandType AType { get; set; }
        public ulong A { get; set; }
        public Label ALabel { get; set; }

        public OperandType BType { get; set; }
        public ulong B { get; set; }
        public Label BLabel { get; set; }

        public OperandType CType { get; set; }
        public ulong C { get; set; }
        public Label CLabel { get; set; }

        public string[] Arguments { get; set; }

        public ulong MaxRegister
        {
            get
            {
                ulong current = 0;

                if (AType == OperandType.Register && A > current) current = A;
                if (BType == OperandType.Register && B > current) current = B;
                if (CType == OperandType.Register && C > current) current = C;

                return current;
            }
        }

        public UrclInstruction(Operation operation)
        {
            Operation = operation;

            AType = OperandType.None;
            A = 0;
            ALabel = null;

            BType = OperandType.None;
            B = 0;
            BLabel = null;

            CType = OperandType.None;
            C = 0;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, string[] args)
        {
            Operation = operation;

            AType = OperandType.None;
            A = 0;
            ALabel = null;

            BType = OperandType.None;
            B = 0;
            BLabel = null;

            CType = OperandType.None;
            C = 0;
            CLabel = null;

            Arguments = args;
        }

        public UrclInstruction(Operation operation, OperandType type, ulong value)
        {
            Operation = operation;

            AType = type;
            A = value;
            ALabel = null;

            BType = OperandType.None;
            B = 0;
            BLabel = null;

            CType = OperandType.None;
            C = 0;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, OperandType type, ulong value, Label label)
        {
            Operation = operation;

            AType = type;
            A = value;
            ALabel = null;

            BType = OperandType.Label;
            B = 0;
            BLabel = label;

            CType = OperandType.None;
            C = 0;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, OperandType aType, ulong a, OperandType bType, ulong b)
        {
            Operation = operation;

            AType = aType;
            A = a;
            ALabel = null;

            BType = bType;
            B = b;
            BLabel = null;

            CType = OperandType.None;
            C = 0;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, OperandType aType, ulong a, OperandType bType, ulong b, OperandType cType, ulong c)
        {
            Operation = operation;

            AType = aType;
            A = a;
            ALabel = null;

            BType = bType;
            B = b;
            BLabel = null;

            CType = cType;
            C = c;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, Label label)
        {
            Operation = operation;

            AType = OperandType.Label;
            A = 0;
            ALabel = label;

            BType = OperandType.None;
            B = 0;
            BLabel = null;

            CType = OperandType.None;
            C = 0;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, Label aLabel, Label bLabel)
        {
            Operation = operation;

            AType = OperandType.Label;
            A = 0;
            ALabel = aLabel;

            BType = OperandType.Label;
            B = 0;
            BLabel = bLabel;

            CType = OperandType.None;
            C = 0;
            CLabel = null;
        }

        public UrclInstruction(Operation operation, Label aLabel, Label bLabel, Label cLabel)
        {
            Operation = operation;

            AType = OperandType.Label;
            A = 0;
            ALabel = aLabel;

            BType = OperandType.Label;
            B = 0;
            BLabel = bLabel;

            CType = OperandType.Label;
            C = 0;
            CLabel = cLabel;
        }

        public UrclInstruction(Operation operation, OperandType[] types, object[] operands)
        {
            if (types.Length != operands.Length) throw new ArgumentException("Array of types must match array of operands.");

            Operation = operation;

            if (types.Length >= 1)
            {
                AType = types[0];

                if (AType == OperandType.Label)
                {
                    A = 0;
                    ALabel = (Label)operands[0];
                }
                else if (AType == OperandType.String)
                {
                    Arguments = operands[0] as string[];
                }
                else
                {
                    A = (ulong)operands[0];
                    ALabel = null;
                }
            }
            else
            {
                AType = OperandType.None;
            }

            if (types.Length >= 2)
            {
                BType = types[1];

                if (BType == OperandType.Label)
                {
                    B = 0;
                    BLabel = (Label)operands[1];
                }
                else
                {
                    B = (ulong)operands[1];
                    BLabel = null;
                }
            }
            else
            {
                BType = OperandType.None;
            }

            if (types.Length >= 3)
            {
                CType = types[2];

                if (CType == OperandType.Label)
                {
                    C = 0;
                    CLabel = (Label)operands[2];
                }
                else
                {
                    C = (ulong)operands[2];
                    CLabel = null;
                }
            }
            else
            {
                CType = OperandType.None;
            }

            if (types.Length >= 4) throw new Exception($"Instructions with {types.Length} operands are not supported.");
        }

        public string GetComponent(int index)
        {
            return index switch
            {
                0 => Operation.ToString(),
                1 => GetOperandString(AType, A, ALabel),
                2 => GetOperandString(BType, B, BLabel),
                3 => GetOperandString(CType, C, CLabel),
                _ => throw new IndexOutOfRangeException(),
            };
        }

        public override string ToString()
        {
            if (Operation == Operation.COMPILER_PRAGMA)
            {
                return $"@{string.Join(' ', Arguments)}";
            }
            else if (Operation == Operation.COMPILER_MARKLABEL)
            {
                return GetOperandString(AType, A, ALabel);
            }
            else if (Operation == Operation.COMPILER_COMMENT)
            {
                return $"//{string.Join($"{Environment.NewLine}//", Arguments)}";
            }
            else
            {
                if (AType == OperandType.None)
                {
                    return Operation.ToString();
                }
                else if (BType == OperandType.None)
                {
                    return $"{Operation} {GetOperandString(AType, A, ALabel)}";
                }
                else if (CType == OperandType.None)
                {
                    return $"{Operation} {GetOperandString(AType, A, ALabel)}, {GetOperandString(BType, B, BLabel)}";
                }
                else
                {
                    return $"{Operation} {GetOperandString(AType, A, ALabel)}, {GetOperandString(BType, B, BLabel)}, {GetOperandString(CType, C, CLabel)}";
                }
            }
        }

        private string GetOperandString(OperandType type, ulong value, Label label)
        {
            return type switch
            {
                OperandType.None => string.Empty,
                OperandType.Register => $"R{value}",
                OperandType.Immediate => value.ToString(),
                OperandType.Label => $".L0x{label.GetHashCode():X}",
                OperandType.String => string.Join(' ', Arguments),
                _ => throw new ArgumentException("Operand must be register or immediate.", nameof(type))
            };
        }
    }
}
