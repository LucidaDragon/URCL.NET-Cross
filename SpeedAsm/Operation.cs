using System;
using System.Collections.Generic;

namespace SpeedAsm
{
    public enum Operation
    {
        [Operation("exit")]
        Exit,
        [Operation("=", true, true)]
        Set,
        [Operation("+", true, true, true)]
        Add,
        [Operation("++", true)]
        Inc,
        [Operation("-", true, true, true)]
        Sub,
        [Operation("--", true)]
        Dec,
        [Operation("*", true, true, true)]
        Mul,
        [Operation("/", true, true, true)]
        Div,
        [Operation("%", true, true, true)]
        Mod,
        [Operation("&", true, true, true)]
        And,
        [Operation("|", true, true, true)]
        Or,
        [Operation("^", true, true, true)]
        Xor,
        [Operation("~", true, true)]
        Not,
        [Operation("&&", true, true, true)]
        BAnd,
        [Operation("||", true, true, true)]
        BOr,
        [Operation("^^", true, true, true)]
        BXor,
        [Operation("!", true, true)]
        BNot,
        [Operation("push", true)]
        Push,
        [Operation("pop", true)]
        Pop,
        [Operation(":", true, destLabel: true)]
        Label,
        [Operation("goto", true, destLabel: true)]
        Branch,
        [Operation("if", true, destLabel: true)]
        BranchIfNotZero,
        [Operation("else", true, destLabel: true)]
        BranchIfZero,
        [Operation("ifcarry", true, destLabel: true)]
        BranchIfCarry,
        [Operation("ifsign", true, destLabel: true)]
        BranchIfSign,
        [Operation("ifnotcarry", true, destLabel: true)]
        BranchIfNotCarry,
        [Operation("ifnotsign", true, destLabel: true)]
        BranchIfNotSign,
        [Operation("call", true, destLabel: true)]
        Call,
        [Operation("return")]
        Return,

        CompilerGenerated
    }

    public static class OperationExtensions
    {
        public static IEnumerable<Operation> Operations => Attributes.Keys;

        private readonly static Dictionary<Operation, OperationAttribute> Attributes = new Dictionary<Operation, OperationAttribute>();

        static OperationExtensions()
        {
            foreach (var field in typeof(Operation).GetFields())
            {
                if (field.FieldType == typeof(Operation))
                {
                    var attribs = field.GetCustomAttributes(typeof(OperationAttribute), false);

                    if (attribs.Length > 0)
                    {
                        Attributes[(Operation)field.GetValue(null)] = (OperationAttribute)attribs[0];
                    }
                }
            }
        }

        public static string GetString(this Operation operation)
        {
            return Attributes[operation].Operator;
        }

        public static OperationAttribute GetAttributes(this Operation operation)
        {
            return Attributes[operation];
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OperationAttribute : Attribute
    {
        public bool ThreeOperand => Destination && Source && Target;
        public bool TwoOperand => Destination && Source && !Target;
        public bool OneOperand => Destination && !Source && !Target;
        public bool ZeroOperand => !Destination && !Source && !Target;

        public readonly string Operator;

        public readonly bool Destination;
        public readonly bool Source;
        public readonly bool Target;

        public readonly bool DestLabel;
        public readonly bool SrcLabel;
        public readonly bool TargLabel;

        public OperationAttribute(string op, bool destination = false, bool source = false, bool target = false, bool destLabel = false, bool srcLabel = false, bool targLabel = false)
        {
            Operator = op;

            Destination = destination;
            Source = source;
            Target = target;

            DestLabel = destLabel;
            SrcLabel = srcLabel;
            TargLabel = targLabel;
        }
    }
}
