using System;

namespace URCL.NET.Compiler
{
    public class ParserError : Exception
    {
        public ParserError(string message) : base(message) { }
    }
}
