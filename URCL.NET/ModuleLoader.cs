using LuC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace URCL.NET
{
    public class ModuleLoader
    {
        private const string DllPattern = "*.dll";
        private const string CurrentDirectory = "./";
        private const string Module = "Module";
        private const string NameFile = "NameFile";
        private const string HandleFile = "HandleFile";
        private const string HandleEmit = "HandleEmit";
        private const string HandleConfiguration = "HandleConfiguration";
        private const string UnknownType = "unknown";

        private readonly Dictionary<string, string> FileNames = new Dictionary<string, string>();
        private readonly Dictionary<string, MethodInfo> FileHandlers = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, MethodInfo> Emitters = new Dictionary<string, MethodInfo>();
        private readonly List<MethodInfo> Configurations = new List<MethodInfo>();

        public void AddFileType(string ext, string name)
        {
            FileNames[ext.ToLower()] = name;
        }

        public string GetFileType(string ext)
        {
            if (FileNames.TryGetValue(ext, out string name))
            {
                return name;
            }
            else
            {
                return UnknownType;
            }
        }

        public void Load(Configuration config)
        {
            var modules = new List<string>();

            modules.AddRange(Directory.GetFiles(CurrentDirectory, DllPattern, SearchOption.AllDirectories));
            if (!string.IsNullOrEmpty(config.Modules)) modules.AddRange(Directory.GetFiles(config.Modules, DllPattern));

            foreach (var module in modules)
            {
                var asm = Assembly.LoadFrom(module);

                foreach (var type in asm.GetTypes())
                {
                    if (type.IsPublic && type.Name.EndsWith(Module) && type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        foreach (var field in type.GetFields())
                        {
                            if (field.Name.StartsWith(NameFile) &&
                                field.Name.Length > NameFile.Length &&
                                field.IsLiteral &&
                                field.FieldType == typeof(string))
                            {
                                FileNames[field.Name.Substring(NameFile.Length).ToLower()] = (string)field.GetValue(null);
                            }
                        }

                        foreach (var prop in type.GetProperties())
                        {
                            if (prop.CanWrite && prop.SetMethod.IsStatic && prop.PropertyType == typeof(Configuration))
                            {
                                prop.SetValue(null, config);
                            }
                        }

                        foreach (var method in type.GetMethods())
                        {
                            if (method.IsPublic && method.ReturnType == typeof(void) && !method.IsConstructor)
                            {
                                var parameters = method.GetParameters();

                                if (method.Name.StartsWith(HandleFile) &&
                                    method.Name.Length > HandleFile.Length &&
                                    parameters.Length == 2 &&
                                    (parameters[0].ParameterType == typeof(Action<string>) ||
                                    parameters[0].ParameterType == typeof(Action<UrclInstruction>)) &&
                                    (parameters[1].ParameterType == typeof(string) ||
                                    parameters[1].ParameterType == typeof(byte) ||
                                    parameters[1].ParameterType == typeof(IEnumerable<string>) ||
                                    parameters[1].ParameterType == typeof(IEnumerable<byte>)))
                                {
                                    FileHandlers[method.Name.Substring(HandleFile.Length).ToLower()] = method;
                                }
                                else if (method.Name.StartsWith(HandleEmit) &&
                                    method.Name.Length > HandleEmit.Length &&
                                    parameters.Length == 5 &&
                                    (parameters[0].ParameterType == typeof(Action<string>) ||
                                    parameters[0].ParameterType == typeof(Action<byte>)) &&
                                    parameters[1].ParameterType == typeof(string) &&
                                    parameters[2].ParameterType == typeof(string) &&
                                    parameters[3].ParameterType == typeof(string) &&
                                    parameters[4].ParameterType == typeof(string))
                                {
                                    Emitters[method.Name.Substring(HandleEmit.Length).ToLower()] = method;
                                }
                                else if (method.Name.StartsWith(HandleEmit) &&
                                    method.Name.Length > HandleEmit.Length &&
                                    parameters.Length == 2 &&
                                    (parameters[0].ParameterType == typeof(Action<string>) ||
                                    parameters[0].ParameterType == typeof(Action<byte>)) &&
                                    (parameters[1].ParameterType == typeof(UrclInstruction) ||
                                    parameters[1].ParameterType == typeof(IEnumerable<UrclInstruction>)))
                                {
                                    Emitters[method.Name.Substring(HandleEmit.Length).ToLower()] = method;
                                }
                                else if (method.Name == HandleConfiguration &&
                                    method.IsStatic &&
                                    parameters.Length == 1 &&
                                    parameters[0].ParameterType == typeof(Configuration))
                                {
                                    Configurations.Add(method);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RunConfigurations(Configuration configuration)
        {
            foreach (var config in Configurations)
            {
                config.Invoke(null, new object[] { configuration });
            }
        }

        public bool ExecuteFileHandler(string ext, IEnumerable<string> lines, Action<string> output, Action<string> error)
        {
            return ExecuteFileHandler(ext, new MemoryStream(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines))), output, error);
        }

        public bool ExecuteFileHandler(string ext, Stream input, Action<string> output, Action<string> error)
        {
            try
            {
                try
                {
                    if (FileHandlers.TryGetValue(ext.ToLower(), out MethodInfo method))
                    {
                        var paramters = method.GetParameters();
                        var target = method.DeclaringType.GetConstructor(Type.EmptyTypes).Invoke(null);

                        object emit = output;

                        if (paramters[0].ParameterType == typeof(Action<UrclInstruction>))
                        {
                            emit = new Action<UrclInstruction>((inst) =>
                            {
                                output(inst.ToString());
                            });
                        }

                        if (paramters[1].ParameterType == typeof(string))
                        {
                            using var reader = new StreamReader(input, Encoding.UTF8, leaveOpen: true);

                            while (!reader.EndOfStream)
                            {
                                string str = reader.ReadLine();

                                if (str is null) break;

                                method.Invoke(target, new object[] { emit, str });
                            }
                        }
                        else if (paramters[1].ParameterType == typeof(IEnumerable<string>))
                        {
                            using var reader = new StreamReader(input, Encoding.UTF8, leaveOpen: true);

                            method.Invoke(target, new object[] { emit, ReadStreamLines(reader) });
                        }
                        else if (paramters[1].ParameterType == typeof(byte))
                        {
                            int value = input.ReadByte();

                            while (value >= 0)
                            {
                                method.Invoke(target, new object[] { emit, (byte)value });
                                value = input.ReadByte();
                            }
                        }
                        else if (paramters[1].ParameterType == typeof(IEnumerable<byte>))
                        {
                            method.Invoke(target, new object[] { emit, ReadStreamBytes(input) });
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            catch (Exception ex)
            {
                error(ex.Message);
                return true;
            }
        }

        public bool ExecuteEmitter(string ext, Action<byte> emit, IEnumerable<UrclInstruction> instructions)
        {
            if (Emitters.TryGetValue(ext.ToLower(), out MethodInfo method))
            {
                var parameters = method.GetParameters();
                var target = method.DeclaringType.GetConstructor(Type.EmptyTypes).Invoke(null);

                object emitter = emit;

                if (parameters[0].ParameterType == typeof(Action<string>))
                {
                    emitter = new Action<string>((str) =>
                    {
                        foreach (var b in Encoding.UTF8.GetBytes(str)) emit(b);
                        foreach (var b in Encoding.UTF8.GetBytes(Environment.NewLine)) emit(b);
                    });
                }

                if (parameters.Length == 2)
                {
                    if (parameters[1].ParameterType == typeof(IEnumerable<UrclInstruction>))
                    {
                        method.Invoke(target, new object[] { emitter, instructions });
                    }
                    else
                    {
                        foreach (var inst in instructions)
                        {
                            method.Invoke(target, new object[] { emitter, inst });
                        }
                    }
                }
                else
                {
                    foreach (var inst in instructions)
                    {
                        method.Invoke(target, new object[]
                        {
                            emit,
                            inst.GetComponent(0),
                            inst.GetComponent(1),
                            inst.GetComponent(2),
                            inst.GetComponent(3)
                        });
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private IEnumerable<string> ReadStreamLines(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();

                if (str is null) break;

                yield return str;
            }
        }

        private IEnumerable<byte> ReadStreamBytes(Stream stream)
        {
            var value = stream.ReadByte();

            while (value >= 0)
            {
                yield return (byte)value;

                value = stream.ReadByte();
            }
        }
    }
}
