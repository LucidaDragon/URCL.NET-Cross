using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace URCL.NET
{
    class AssemblyCompiler
    {
        public IEnumerable<TypeCompiler> Types => MyTypes;

        public Label EntryPoint { get; private set; } = null;

        private readonly List<TypeCompiler> MyTypes = new List<TypeCompiler>();

        public AssemblyCompiler(Configuration configuration, ModuleDefinition assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                MyTypes.Add(new TypeCompiler(configuration, this, type));
            }

            if (assembly.EntryPoint != null)
            {
                var compiler = GetMethodCompiler(assembly.EntryPoint);

                if (compiler == null)
                {
                    throw new NotImplementedException($"Entry point {assembly.EntryPoint.DeclaringType.FullName}:{assembly.EntryPoint.Name} could not be found.");
                }
                else
                {
                    EntryPoint = compiler.MethodLabel;
                }
            }
        }

        public MethodCompiler GetMethodCompiler(MethodDefinition method, TypeCompiler caller = null)
        {
            foreach (var type in Types)
            {
                if (type != caller)
                {
                    var compiler = type.GetMethodCompiler(method, false);

                    if (compiler != null)
                    {
                        return compiler;
                    }
                }
            }

            return null;
        }

        public void PreCompile()
        {
            foreach (var type in Types)
            {
                type.PreCompile();
            }
        }

        public void Resolve()
        {
            foreach (var type in Types)
            {
                type.Resolve();
            }
        }
    }
}
