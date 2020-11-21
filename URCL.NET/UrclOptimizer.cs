using System.Collections.Generic;

namespace URCL.NET
{
    public class UrclOptimizer
    {
        public bool All
        {
            set
            {
                CullRedundantStackOps = value;
                CullCreateLabel = value;
                CullComments = value;
                CullPragmas = value;
                ReplaceImmZeroWithZeroRegister = value;
            }
        }

        public bool CullRedundantStackOps { get; set; } = false;
        public bool CullCreateLabel { get; set; } = false;
        public bool CullComments { get; set; } = false;
        public bool CullPragmas { get; set; } = false;

        public bool ReplaceImmZeroWithZeroRegister { get; set; } = false;

        public UrclInstruction[] Optimize(UrclInstruction[] instructions)
        {
            var insts = new List<UrclInstruction>(instructions);

            for (int i = 0; i < insts.Count; i++)
            {
                var current = insts[i];

                UrclInstruction next = null;
                int gap = 1;
                for (int j = 1; i + j < insts.Count; j++, gap++)
                {
                    next = insts[i + j];

                    if (next.Operation != Operation.COMPILER_COMMENT) break;
                }

                if (CullRedundantStackOps && current.Operation == Operation.PSH)
                {
                    if (next != null && next.Operation == Operation.POP)
                    {
                        if (current.A == next.A)
                        {
                            RemoveInstructionRange(insts, i, gap + 1, CullComments);
                        }
                        else
                        {
                            RemoveInstructionRange(insts, i + 1, gap, CullComments);

                            current.Operation = Operation.MOV;

                            current.BType = current.AType;
                            current.B = current.A;
                            current.BLabel = current.ALabel;

                            current.AType = next.AType;
                            current.A = next.A;
                            current.ALabel = next.ALabel;
                        }

                        i = 0;
                    }
                }
                else if (ReplaceImmZeroWithZeroRegister && current.Operation == Operation.IMM)
                {
                    if (current.BType == OperandType.Immediate && current.B == 0)
                    {
                        insts[i] = new UrclInstruction(Operation.MOV, insts[i].AType, insts[i].A, OperandType.Register, 0);
                    }
                }
                else if (CullCreateLabel && current.Operation == Operation.COMPILER_CREATELABEL)
                {
                    insts.RemoveAt(i);
                    i = 0;
                }
                else if (CullComments && current.Operation == Operation.COMPILER_COMMENT)
                {
                    insts.RemoveAt(i);
                    i = 0;
                }
                else if (CullPragmas && current.Operation == Operation.COMPILER_PRAGMA)
                {
                    insts.RemoveAt(i);
                    i = 0;
                }
            }

            return insts.ToArray();
        }

        private void RemoveInstructionRange(List<UrclInstruction> instructions, int start, int length, bool cullComments)
        {
            for (int i = 0; i < length; i++)
            {
                if (instructions[start].Operation != Operation.COMPILER_COMMENT || cullComments)
                {
                    instructions.RemoveAt(start);
                }
                else
                {
                    start++;
                }
            }
        }
    }
}
