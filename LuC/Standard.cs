using LuC.Tree;

namespace LuC
{
    public static class Standard
    {
        public static void AddStandardDefaults(this Compiler compiler)
        {
            compiler.AddDefaultNamespace(new Namespace(0, 0, string.Empty, new[]
            {
                Function.CreateNativeFunction("+", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.AddInt();
                }, true),
                Function.CreateNativeFunction("-", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.SubtractInt();
                }, true),
                Function.CreateNativeFunction("*", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.MultiplyInt();
                }, true),
                Function.CreateNativeFunction("/", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.DivideInt();
                }, true),
                Function.CreateNativeFunction("%", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.ModuloInt();
                }, true),
                Function.CreateNativeFunction("<<", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.LeftShiftInt();
                }, true),
                Function.CreateNativeFunction(">>", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.RightShiftInt();
                }, true),
                Function.CreateNativeFunction("&", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.AndInt();
                }, true),
                Function.CreateNativeFunction("|", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.OrInt();
                }, true),
                Function.CreateNativeFunction("^", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.XorInt();
                }, true),
                Function.CreateNativeFunction("~", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a")
                }, (emit, f) =>
                {
                    emit.NotInt();
                }, true),
                Function.CreateNativeFunction("&&", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.AndBoolean();
                }, true),
                Function.CreateNativeFunction("||", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.OrBoolean();
                }, true),
                Function.CreateNativeFunction("^^", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    emit.XorBoolean();
                }, true),
                Function.CreateNativeFunction("!", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a")
                }, (emit, f) =>
                {
                    emit.NotBoolean();
                }, true),
                Function.CreateNativeFunction(">", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    var failed = emit.CreateLabel();
                    var end = emit.CreateLabel();

                    emit.SubtractInt();
                    emit.BranchIfNegative(failed);
                    emit.BranchIfZero(failed);
                    emit.LoadConstant(1);
                    emit.Branch(end);
                    emit.MarkLabel(failed);
                    emit.LoadConstant(0);
                    emit.MarkLabel(end);
                }, true),
                Function.CreateNativeFunction("<", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    var failed = emit.CreateLabel();
                    var end = emit.CreateLabel();

                    emit.SubtractInt();
                    emit.BranchIfPositive(failed);
                    emit.LoadConstant(1);
                    emit.Branch(end);
                    emit.MarkLabel(failed);
                    emit.LoadConstant(0);
                    emit.MarkLabel(end);
                }, true),
                Function.CreateNativeFunction(">=", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    var failed = emit.CreateLabel();
                    var end = emit.CreateLabel();

                    emit.SubtractInt();
                    emit.BranchIfNegative(failed);
                    emit.LoadConstant(1);
                    emit.Branch(end);
                    emit.MarkLabel(failed);
                    emit.LoadConstant(0);
                    emit.MarkLabel(end);
                }, true),
                Function.CreateNativeFunction("<=", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    var failed = emit.CreateLabel();
                    var success = emit.CreateLabel();
                    var end = emit.CreateLabel();

                    emit.SubtractInt();
                    emit.BranchIfZero(success);
                    emit.BranchIfPositive(failed);
                    emit.MarkLabel(success);
                    emit.LoadConstant(1);
                    emit.Branch(end);
                    emit.MarkLabel(failed);
                    emit.LoadConstant(0);
                    emit.MarkLabel(end);
                }, true),
                Function.CreateNativeFunction("==", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    var failed = emit.CreateLabel();
                    var end = emit.CreateLabel();

                    emit.SubtractInt();
                    emit.BranchIfNotZero(failed);
                    emit.LoadConstant(1);
                    emit.Branch(end);
                    emit.MarkLabel(failed);
                    emit.LoadConstant(0);
                    emit.MarkLabel(end);
                }, true),
                Function.CreateNativeFunction("!=", Compiler.NativeInt, new[]
                {
                    new Field(0, 0, compiler, Compiler.NativeInt, "a"),
                    new Field(0, 0, compiler, Compiler.NativeInt, "b"),
                }, (emit, f) =>
                {
                    var failed = emit.CreateLabel();
                    var end = emit.CreateLabel();

                    emit.SubtractInt();
                    emit.BranchIfZero(failed);
                    emit.LoadConstant(1);
                    emit.Branch(end);
                    emit.MarkLabel(failed);
                    emit.LoadConstant(0);
                    emit.MarkLabel(end);
                }, true)
            }));
        }
    }
}
