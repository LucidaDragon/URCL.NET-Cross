using System;

namespace URCL.NET.PlatformIL
{
    public class StdLib
    {
        public Label Malloc { get; } = new Label();
        public Label Free { get; } = new Label();
        public Label Print { get; } = new Label();
        public Label OutOfMemory { get; set; } = new Label();

        public void CallStd(Action<UrclInstruction> emit, Label stdFunc)
        {
            var returnPoint = new Label();
            emit(new UrclInstruction(Operation.PSH, OperandType.Register, 1));
            emit(new UrclInstruction(Operation.IMM, OperandType.Register, 1, returnPoint));
            emit(new UrclInstruction(Operation.BRA, stdFunc));
            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, returnPoint));
            emit(new UrclInstruction(Operation.POP, OperandType.Register, 1));
        }

        public void Emit(Action<UrclInstruction> emit)
        {
            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, Malloc));
            emit(new UrclInstruction(Operation.IMM, OperandType.Register, 2, OperandType.Immediate, 0));
            emit(new UrclInstruction(Operation.BRA, OperandType.Register, 1));

            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, Free));
            emit(new UrclInstruction(Operation.BRA, OperandType.Register, 1));

            var exit = new Label();
            var loop = new Label();
            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, Print));
            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, loop));
            emit(new UrclInstruction(Operation.LOD, OperandType.Register, 3, OperandType.Register, 2));
            emit(new UrclInstruction(Operation.OR, OperandType.Register, 3, OperandType.Register, 3, OperandType.Register, 3));
            emit(new UrclInstruction(Operation.BRZ, exit));
            emit(new UrclInstruction(Operation.OUT, OperandType.Immediate, 79, OperandType.Register, 3));
            emit(new UrclInstruction(Operation.INC, OperandType.Register, 2, OperandType.Register, 2));
            emit(new UrclInstruction(Operation.BRA, loop));
            emit(new UrclInstruction(Operation.COMPILER_MARKLABEL, exit));

        }
    }
}
