using Mono.Cecil;
using System.Collections.Generic;

namespace URCL.NET
{
    class TypeCompiler
    {
        public IEnumerable<MethodCompiler> Methods => MyMethods.Values;
        public AssemblyCompiler Assembly { get; }

        private readonly Dictionary<MethodDefinition, MethodCompiler> MyMethods = new Dictionary<MethodDefinition, MethodCompiler>();

        public TypeCompiler(Configuration configuration, AssemblyCompiler assembly, TypeDefinition type)
        {
            Assembly = assembly;

            foreach (var method in type.Methods)
            {
                MyMethods.Add(method, new MethodCompiler(configuration, this, method));
            }
        }

        public MethodCompiler GetMethodCompiler(MethodDefinition method, bool searchAssembly)
        {
            if (MyMethods.TryGetValue(method, out MethodCompiler compiler))
            {
                return compiler;
            }
            else if (searchAssembly)
            {
                return Assembly.GetMethodCompiler(method);
            }
            else
            {
                return null;
            }
        }

        public void PreCompile()
        {
            foreach (var method in Methods)
            {
                method.PreCompile();
            }
        }

        public void Resolve()
        {
            foreach (var method in Methods)
            {
                method.Resolve();
            }
        }
    }
}
