using System;
using LuC.Tree;

namespace LuC
{
    public class SourceError : Exception
    {
        public const string InvalidSyntax = "Invalid syntax.";
        public const string InvalidParameterSyntax = "Invalid function parameter syntax.";
        public const string WrongContext = "Syntax is not valid in the current context.";
        public const string UnexpectedEnd = "Unexpected end of statement.";
        public const string UnknownStatement = "Could not compile statement.";
        public const string MissingAttribute = "Statement is missing a required attribute.";
        public const string MissingBlock = "Statement requires a block, but no block was found.";
        public const string EmptyExpression = "Expression is empty.";
        public const string InvalidExpression = "Expression is invalid.";
        public const string InvalidLiteral = "Literal is invalid.";
        public const string InvalidField = "Field is invalid";
        public const string MemberAlreadyExists = "Member with the same name already exists.";
        public const string UndefinedIdentifier = "Identifier is not defined.";
        public const string NamespaceNotResolved = "Namespace of member could not be resolved.";
        public const string FunctionNotResolved = "Function of member could not be resolved.";
        public const string StatementNotResolved = "Statement of member could not be resolved.";
        public const string TooManyValuesOnStack = "Stack frame contains more than 65535 values.";

        public int Start { get; set; }
        public int Length { get; set; }

        public SourceError(int start, int length, string message) : base(message)
        {
            Start = start;
            Length = length;
        }

        public SourceError(TreeObject causer, string message) : base(message)
        {
            Start = causer.Start;
            Length = causer.Length;
        }

        public int GetLine(string src)
        {
            int line = 0;

            for (int i = 0; (i < src.Length && i != Start); i++)
            {
                if (src[i] == '\n') line++;
            }

            return line + 1;
        }

        public void GetLineAndColumn(string src, out int line, out int column)
        {
            int lastBreak = 0;
            column = 0;
            line = 0;

            for (int i = 0; (i < src.Length && i != Start); i++)
            {
                if (src[i] == '\n')
                {
                    lastBreak = i;
                    line++;
                }

                column = i - lastBreak;
            }

            line++;
            column++;
        }
    }
}
