using System;
using System.Collections.Generic;
using System.IO;

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
            Emit(emit, instructions, false);
        }

        public void HandleEmitDll(Action<byte> emit, IEnumerable<UrclInstruction> instructions)
        {
            Emit(emit, instructions, true);
        }

        private void Emit(Action<byte> emit, IEnumerable<UrclInstruction> instructions, bool isLibrary)
        {
            var output = Path.GetFileNameWithoutExtension(Configuration.Output);
            
            foreach (var b in Emitter.Emit(output, instructions, isLibrary))
            {
                emit(b);
            }

            File.WriteAllText($"{Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(Configuration.Output))).Replace('\\', '/')}/{output}.runtimeconfig.json", RuntimeJson);
        }
    }
}
