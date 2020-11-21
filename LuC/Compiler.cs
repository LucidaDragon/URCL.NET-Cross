using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LuC.Tree;

namespace LuC
{
    public class Compiler
    {
        public const string TypeOf = "___typeof";
        public const string NativeInt = "int";
        public const string Void = "void";

        private readonly List<Function> GlobalFunctions = new List<Function>();
        private readonly DataType GlobalNativeInt = new NativeIntType();
        private readonly DataType GlobalVoid = new VoidType();
        private readonly Dictionary<string, Namespace> Namespaces = new Dictionary<string, Namespace>();
        private readonly Dictionary<Function, FunctionLayout> FunctionLayouts = new Dictionary<Function, FunctionLayout>();

        public void Emit(IEmitter emit, string entrypoint)
        {
            var root = new InvalidObject();
            var f = ResolveFunction(root, entrypoint, new string[0]);
            Emit(emit, f);

            var emitted = new List<Function> { f };

            foreach (var ns in Namespaces.Values)
            {
                foreach (var func in ns.Functions)
                {
                    if (!emitted.Contains(func) && !func.Inline)
                    {
                        Emit(emit, func);

                        emitted.Add(func);
                    }
                }
            }
        }

        public void Emit(IEmitter emit, Function f)
        {
            emit.BeginFunction(f);

            EmitBeginFunction(emit, f);

            if (f.Native && !f.Inline)
            {
                f.NativeBody.Commit(emit, true);
            }
            else
            {
                Emit(emit, f.Body);
            }

            emit.EndFunction(f);
        }

        public void Emit(IEmitter emit, IEnumerable<Statement> body)
        {
            foreach (var s in body)
            {
                Emit(emit, s);
            }
        }

        public void Emit(IEmitter emit, Statement s)
        {
            if (s is WhileStatement ws)
            {
                var loop = emit.CreateLabel();
                var end = emit.CreateLabel();

                emit.MarkLabel(loop);

                Emit(emit, ws.Condition);

                emit.BranchIfZero(end);

                Emit(emit, ws.Body);

                emit.Branch(loop);

                emit.MarkLabel(end);
            }
            else if (s is IfStatement ifs)
            {
                var falseLabel = emit.CreateLabel();
                var endLabel = emit.CreateLabel();

                Emit(emit, ifs.Condition);

                emit.BranchIfZero(falseLabel);

                Emit(emit, ifs.TrueBody);

                if (ifs.FalseBody.Any())
                {
                    emit.Branch(endLabel);

                    emit.MarkLabel(falseLabel);

                    Emit(emit, ifs.FalseBody);

                    emit.MarkLabel(endLabel);
                }
                else
                {
                    emit.MarkLabel(falseLabel);
                    emit.MarkLabel(endLabel);
                }
            }
            else if (s is ReturnStatement ret)
            {
                if (ret.Result != null)
                {
                    Emit(emit, ret.Result);
                }

                emit.Return(ret.GetFunction());
            }
            else if (s is ExpressionStatement e)
            {
                Emit(emit, e.Expression);

                EmitPop(emit, ResolveOneType(e.Expression, e.Expression.ReturnType));
            }
            else if (!(s is LocalDeclarationStatement))
            {
                throw new SourceError(s, SourceError.UnknownStatement);
            }
        }

        public void Emit(IEmitter emit, Expression e)
        {
            if (e is LiteralExpression literal)
            {
                ulong value;

                switch (IdentifyLiteral(literal))
                {
                    case Token.String:
                        throw new SourceError(literal, SourceError.InvalidLiteral);
                    case Token.Char:
                        if (literal.Value.StartsWith('\'') && literal.Value.EndsWith('\''))
                        {
                            var chr = literal.Value[1..^1];

                            if (chr.StartsWith('\\'))
                            {
                                chr = chr.Substring(1);

                                if (chr.Length > 1)
                                {
                                    if (chr.StartsWith('u'))
                                    {
                                        if (ulong.TryParse(chr.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                                        {
                                            break;
                                        }
                                    }
                                    else if (chr.Length == 2 && (chr[1] == '\\' || chr[1] == '\''))
                                    {
                                        value = chr[1];
                                        break;
                                    }
                                }
                                else
                                {
                                    value = chr[0];
                                    break;
                                }
                            }
                        }
                        throw new SourceError(literal, SourceError.InvalidLiteral);
                    case Token.Number:
                        if (!ulong.TryParse(literal.Value, out value)) throw new SourceError(literal, SourceError.InvalidLiteral);
                        break;
                    case Token.HexNumber:
                        if (!(literal.Value.StartsWith("0x") && ulong.TryParse(literal.Value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))) throw new SourceError(literal, SourceError.InvalidLiteral);
                        break;
                    default:
                        throw new SourceError(literal, SourceError.InvalidLiteral);
                }

                emit.LoadConstant(value);
            }
            else if (e is IdentifierExpression identifier)
            {
                var layout = GetFunctionLayout(e.GetFunction());
                var local = layout.ResolveField(e, identifier.Name);

                EmitLoadType(emit, layout.GetFieldStart(local), layout.GetField(local).Type);
            }
            else if (e is BinaryExpression binary)
            {
                Emit(emit, binary.A);
                Emit(emit, binary.B);

                EmitCall(emit, ResolveFunction(binary, binary.Operator));
            }
            else if (e is CallExpression call)
            {
                foreach (var child in call.Arguments)
                {
                    Emit(emit, child);
                }

                EmitCall(emit, ResolveFunction(call, call.Reference));
            }
            else
            {
                throw new SourceError(e, SourceError.InvalidExpression);
            }
        }

        public void EmitCall(IEmitter emit, Function f)
        {
            if (f.Native && f.Inline)
            {
                Emit(emit, f.Body);
            }
            else
            {
                emit.LoadFunctionPointer(f);
                emit.Call();
            }
        }

        public void EmitBeginFunction(IEmitter emit, Function f)
        {
            var layout = GetFunctionLayout(f);

            for (int i = layout.ParameterStarts.Length - 1; i >= 0; i--)
            {
                EmitStoreType(emit, layout.ParameterStarts[i], layout.ParameterFields[i].Type);
            }

            for (int i = 0; i < layout.LocalStarts.Length; i++)
            {
                var type = layout.LocalFields[i].Type;
                for (ulong j = 0; j < type.Size; j++) emit.LoadConstant(0);
                EmitStoreType(emit, layout.LocalStarts[i], type);
            }
        }

        public void EmitLoadType(IEmitter emit, ulong startIndex, DataType type)
        {
            for (ulong i = 0; i < type.Size; i++)
            {
                emit.LoadLocal(startIndex + i);
            }
        }

        public void EmitStoreType(IEmitter emit, ulong startIndex, DataType type)
        {
            for (ulong i = type.Size; i > 0; i--)
            {
                emit.StoreLocal(startIndex + (i - 1));
            }
        }

        public void EmitPop(IEmitter emit, DataType type)
        {
            for (ulong i = 0; i < type.Size; i++) emit.Pop();
        }

        public void Compile(IEnumerable<Namespace> namespaces)
        {
            foreach (var ns in namespaces)
            {
                Compile(ns);
            }
        }

        public void Compile(Namespace ns)
        {
            foreach (var f in ns.Functions)
            {
                FunctionLayouts[f] = new FunctionLayout(f);
            }

            if (Namespaces.TryGetValue(ns.Name, out Namespace current))
            {
                current.Merge(ns);
            }
            else
            {
                Namespaces.Add(ns.Name, ns);
            }
        }

        public void AddDefaultNamespace(Namespace ns)
        {
            if (!Namespaces.Values.Contains(ns)) Compile(ns);

            foreach (var f in ns.Functions)
            {
                Compile(f);
            }
        }

        public void Compile(Function f)
        {
            GlobalFunctions.Add(f);
        }

        public DataType ResolveOneType(TreeObject context, string type)
        {
            var types = ResolveType(context, type);

            if (types.Any())
            {
                if (types.Count() == 1)
                {
                    return types.First();
                }
                else
                {
                    throw new SourceError(context, $"\"{type}\" is ambiguous.");
                }
            }
            else
            {
                throw new SourceError(context, $"Could not resolve type \"{type}\".");
            }
        }

        public IEnumerable<DataType> ResolveType(TreeObject context, string type)
        {
            if (type == NativeInt) return new[] { GlobalNativeInt };
            if (type == Void) return new[] { GlobalVoid };

            var match = Regex.Match(type, Tokens.Identifier);

            if (match.Success && match.Index == 0)
            {
                if (match.Value == TypeOf)
                {
                    var subType = type.Substring(match.Length);

                    match = Regex.Match(subType, Tokens.MatchBrackets);

                    if (match.Success && match.Index == 0)
                    {
                        return ResolveType(context, match.Groups[1].Value);
                    }
                }
                else
                {
                    var name = match.Value;
                    var subType = type.Substring(match.Length);

                    match = Regex.Match(subType, Tokens.MatchBrackets);

                    if (match.Success && match.Index == 0)
                    {
                        var value = match.Value.Trim();
                        return ResolveType(context, ResolveFunction(context, name, value[1..^1].Split(',')).ReturnType);
                    }
                    else
                    {
                        var ns = context.GetNamespace(false);
                        var f = context.GetFunction(false);
                        IEnumerable<Field> fields = new Field[0];
                        IEnumerable<DataType> localContext = new DataType[0];

                        if (f != null)
                        {
                            fields = f.Parameters.Concat(f.Locals);
                        }

                        if (ns != null)
                        {
                            localContext = ns.Functions.Where(f => f.Name == name).Select(f => new FunctionPointer(f));
                        }

                        return Namespaces.Values
                            .Where(otherNs => (ns is null || !otherNs.Equals(ns)) && name.StartsWith($"{otherNs.Name}."))
                            .SelectMany(otherNs => otherNs.Functions.Where(f => f.Name == name.Substring(otherNs.Name.Length + 1)))
                            .Select(f => new FunctionPointer(f))
                            .Concat(
                                fields.Where(field => field.Name == name)
                                .Select(field => field.Type)
                            );
                    }
                }
            }
            else
            {
                match = Regex.Match(type, Tokens.Operator);

                if (match.Success && match.Index == 0)
                {
                    var name = match.Value;
                    var subType = type.Substring(match.Length);

                    match = Regex.Match(subType, Tokens.MatchBrackets);

                    if (match.Success && match.Index == 0)
                    {
                        return ResolveType(context, ResolveFunction(context, name, match.Value.Split(',')).ReturnType);
                    }
                }
            }

            throw new SourceError(context, $"Could not resolve type \"{type}\".");
        }

        public Function ResolveFunction(TreeObject context, FunctionReference reference)
        {
            return ResolveFunction(context, reference.Name, reference.Arguments);
        }

        public Function ResolveFunction(TreeObject context, string name, IEnumerable<string> args)
        {
            var ns = context.GetNamespace(false);
            IEnumerable<Function> functions;

            if (ns != null)
            {
                if (name.StartsWith($"{ns.Name}.")) name = name.Substring(ns.Name.Length + 1);

                functions = context.GetNamespace().Functions.Where(f => f.Name == name && MatchFunctionArguments(context, f, args));

                if (functions.Any())
                {
                    if (functions.Count() == 1)
                    {
                        return functions.First();
                    }
                    else
                    {
                        throw new SourceError(context, $"\"{name}({string.Join(',', args)})\" is ambiguous.");
                    }
                }
            }

            functions = Namespaces.Values
                .Where(otherNs => !otherNs.Equals(ns) && name.StartsWith($"{otherNs.Name}."))
                .SelectMany(otherNs => otherNs.Functions.Where(f => f.Name == name.Substring(otherNs.Name.Length + 1)))
                .Concat(GlobalFunctions.Where(f => f.Name == name))
                .Where(f => MatchFunctionArguments(context, f, args)).ToArray();

            if (functions.Any())
            {
                if (functions.Count() == 1)
                {
                    return functions.First();
                }
                else
                {
                    throw new SourceError(context, $"\"{name}({string.Join(',', args)})\" is ambiguous.");
                }
            }
            else
            {
                throw new SourceError(context, $"Could not resolve function \"{name}({string.Join(',', args)})\".");
            }
        }

        private bool MatchFunctionArguments(TreeObject context, Function function, IEnumerable<string> arguments)
        {
            if (!function.Parameters.Any())
            {
                return !arguments.Any();
            }

            if (function.Parameters.Count() != arguments.Count())
            {
                return false;
            }

            return function.Parameters
                .Zip(arguments, (a, b) => (a, b))
                .Select(f => CheckTypeEquality(f.a.Type, ResolveOneType(context, f.b)))
                .Aggregate((a, b) => a && b);
        }

        private FunctionLayout GetFunctionLayout(Function f)
        {
            if (!FunctionLayouts.TryGetValue(f, out FunctionLayout layout))
            {
                layout = new FunctionLayout(f);
                FunctionLayouts.Add(f, layout);
            }

            return layout;
        }

        private static bool CheckTypeEquality(DataType a, DataType b)
        {
            if (a is UnresolvedDataType ua)
            {
                a = ua.Resolve();
            }
            
            if (b is UnresolvedDataType ub)
            {
                b = ub.Resolve();
            }

            return a.Equals(b);
        }

        private static Token IdentifyLiteral(LiteralExpression literal)
        {
            var lexed = Parser.Lex(literal.Value);

            if (lexed.Count == 1)
            {
                return lexed[0].Type;
            }
            else
            {
                throw new SourceError(literal, SourceError.InvalidLiteral);
            }
        }

        private class FunctionLayout
        {
            public ulong[] ParameterStarts { get; }
            public Field[] ParameterFields { get; }
            public ulong[] LocalStarts { get; }
            public Field[] LocalFields { get; }

            public int Count => ParameterStarts.Length + LocalStarts.Length;

            public FunctionLayout(Function f)
            {
                ParameterStarts = new ulong[f.Parameters.Length];
                ParameterFields = new Field[f.Parameters.Length];
                LocalStarts = new ulong[f.Locals.Length];
                LocalFields = new Field[f.Locals.Length];

                ulong address = 0;

                for (int i = 0; i < f.Parameters.Length; i++)
                {
                    var field = f.Parameters[i];
                    ParameterStarts[i] = address;
                    ParameterFields[i] = field;
                    address += field.Type.Size;
                }

                for (int i = 0; i < f.Locals.Length; i++)
                {
                    var field = f.Locals[i];
                    LocalStarts[i] = address;
                    LocalFields[i] = field;
                    address += field.Type.Size;
                }
            }

            public int ResolveField(TreeObject context, string name)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (GetField(i).Name == name)
                    {
                        return i;
                    }
                }

                throw new SourceError(context, SourceError.UndefinedIdentifier);
            }

            public ulong GetFieldStart(int index)
            {
                if (index < ParameterStarts.Length)
                {
                    return ParameterStarts[index];
                }
                else
                {
                    return LocalStarts[index - ParameterStarts.Length];
                }
            }

            public Field GetField(int index)
            {
                if (index < ParameterFields.Length)
                {
                    return ParameterFields[index];
                }
                else
                {
                    return LocalFields[index - ParameterFields.Length];
                }
            }
        }

        private class NativeIntType : DataType
        {
            public override ulong Size => 1;

            public NativeIntType() : base(0, 0) { }
        }

        private class VoidType : DataType
        {
            public override ulong Size => 0;

            public VoidType() : base(0, 0) { }
        }
    }
}
