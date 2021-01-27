using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace URCL.NET.PlatformIL
{
    public class ILModule
    {
        public const string NameFileExe = ".NET Portable Executable";
        public const string NameFileDll = ".NET Dynamic Link Library";

        public static Configuration Configuration { get; set; }

        private const string RuntimeJson = "{\"runtimeOptions\":{\"tfm\":\"netcoreapp3.1\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"3.1.0\"}}}";
        private readonly ILEmitter Emitter = new ILEmitter();

        public void HandleEmitExe(Action<byte> emit, IEnumerable<UrclInstruction> instructions)
        {
            EmitIL(emit, instructions, false);
        }

        public void HandleEmitDll(Action<byte> emit, IEnumerable<UrclInstruction> instructions)
        {
            EmitIL(emit, instructions, true);
        }

        public void HandleFileExe(Action<UrclInstruction> emit, IEnumerable<byte> assembly)
        {
            EmitURCL(emit, assembly, false);
        }

        public void HandleFileDll(Action<UrclInstruction> emit, IEnumerable<byte> assembly)
        {
            EmitURCL(emit, assembly, true);
        }

        private void EmitIL(Action<byte> emit, IEnumerable<UrclInstruction> instructions, bool isLibrary)
        {
            var output = Path.GetFileNameWithoutExtension(Configuration.Output);
            
            foreach (var b in Emitter.Emit(output, instructions, isLibrary))
            {
                emit(b);
            }

            File.WriteAllText($"{Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(Configuration.Output))).Replace('\\', '/')}/{output}.runtimeconfig.json", RuntimeJson);
        }

        private void EmitURCL(Action<UrclInstruction> emit, IEnumerable<byte> assembly, bool isLibrary)
        {
            var buffer = new List<UrclInstruction>();

            var module = ModuleDefinition.ReadModule(new MemoryStream(assembly.ToArray()));

            var std = new StdLib();

            var halt = new Label();
            var begin = new Label();

            buffer.Add(new UrclInstruction(Operation.IMM, OperandType.Register, 1, halt));
            buffer.Add(new UrclInstruction(Operation.BRA, begin));
            buffer.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, halt));
            buffer.Add(new UrclInstruction(Operation.HLT));
            buffer.Add(new UrclInstruction(Operation.COMPILER_MARKLABEL, begin));

            foreach (var type in module.Types)
            {
                var urclType = new UrclType(type);

                urclType.Emit(buffer.Add, std, Configuration);
            }

            std.Emit(buffer.Add);

            var optimizer = new UrclOptimizer
            {
                All = true,
                CullComments = false
            };

            foreach (var inst in optimizer.Optimize(buffer.ToArray()))
            {
                emit(inst);
            }
        }
    }
}
