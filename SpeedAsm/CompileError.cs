using System;

namespace SpeedAsm
{
    public class CompileError : Exception
    {
        public const string InvalidSyntax = "Statement is not valid.";
        public const string ImmediateAsLabel = "Immediate can not be used as a label.";
        public const string ImmediateAsVariable = "Immediate can not be used as a variable.";
        public const string LabelAsVariable = "Label can not be used as a variable.";
        public const string VariableAsLabel = "Variable can not be used as a label.";

        public CompileError(string message) : base(message) { }
    }
}
