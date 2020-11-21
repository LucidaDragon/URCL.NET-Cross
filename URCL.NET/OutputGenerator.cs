using System;
using System.Collections.Generic;
using System.IO;

namespace URCL.NET
{
    class OutputGenerator
    {
        private readonly List<UrclInstruction> Instructions = new List<UrclInstruction>();
        private readonly Dictionary<Label, int> Labels = new Dictionary<Label, int>();
        private readonly Dictionary<int, string[]> Comments = new Dictionary<int, string[]>();

        private ulong MinRegisters = 0;
        private ulong MinMemory = 0;

        public void EntryPoint(Configuration configuration, Label label)
        {
            var compiler = new MethodCompiler(configuration, null, null);

            compiler.Comment("Initialize Stack and Base Pointer");
            //compiler.Emit(Operation.IMM, OperandType.Register, compiler.Registers.StackPointer, OperandType.Immediate, configuration.AvailableMemory);
            //compiler.Emit(Operation.MOV, OperandType.Register, compiler.Registers.BasePointer, OperandType.Register, compiler.Registers.StackPointer);

            if (label != null)
            {
                compiler.Comment("Push Entrypoint Argument");
                //compiler.Push(OperandType.Immediate, 0);

                compiler.Comment("Jump to Entrypoint");
                compiler.Branch(label);
            }

            Instructions.AddRange(compiler.Body);

            foreach (var comments in compiler.Comments)
            {
                Comments.Add(comments.Key, comments.Value.ToArray());
            }

            MinMemory = configuration.AvailableMemory;
        }

        public void AddBlock(AssemblyCompiler assembly)
        {
            foreach (var type in assembly.Types)
            {
                foreach (var method in type.Methods)
                {
                    foreach (var label in method.Labels)
                    {
                        //Labels.Add(label.Key, label.Value + Instructions.Count);
                    }

                    foreach (var comments in method.Comments)
                    {
                        Comments.Add(comments.Key + Instructions.Count, comments.Value.ToArray());
                    }

                    Instructions.AddRange(method.Body);
                }
            }

            foreach (var op in Instructions)
            {
                if (op.AType == OperandType.Label)
                {
                    op.A = Resolve(op.ALabel);
                    op.AType = OperandType.Immediate;
                }

                if (op.BType == OperandType.Label)
                {
                    op.B = Resolve(op.BLabel);
                    op.BType = OperandType.Immediate;
                }

                if (op.CType == OperandType.Label)
                {
                    op.C = Resolve(op.CLabel);
                    op.CType = OperandType.Immediate;
                }

                if (op.MaxRegister > MinRegisters)
                {
                    MinRegisters = op.MaxRegister;
                }
            }
        }

        public void Output(TextWriter writer)
        {
            writer.WriteLine($"MINREG {MinRegisters}");
            writer.WriteLine($"MINRAM {MinMemory}");

            for (int i = 0; i < Instructions.Count; i++)
            {
                if (Comments.TryGetValue(i, out string[] comments))
                {
                    foreach (var comment in comments)
                    {
                        writer.WriteLine($"//{comment}");
                    }
                }

                writer.WriteLine(Instructions[i].ToString());
            }
        }

        private ulong Resolve(Label label)
        {
            if (Labels.TryGetValue(label, out int address))
            {
                return (ulong)address;
            }
            else
            {
                throw new NotImplementedException("Unmarked label found.");
            }
        }
    }
}
