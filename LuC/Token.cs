namespace LuC
{
    public enum Token
    {
        Invalid,
        [Token(@"\s+", ' ')]
        Whitespace,
        [Token("namespace", 'n')]
        KeywordNamespace,
        [Token("struct", 's')]
        KeywordStructure,
        [Token("operator", 'o')]
        KeywordOperator,
        [Token("if", 'f')]
        KeywordIf,
        [Token("else", 'e')]
        KeywordElse,
        [Token("while", 'w')]
        KeywordWhile,
        [Token("return", 'r')]
        KeywordReturn,
        [Token(@"\(", '(')]
        LeftBracket,
        [Token(@"\)", ')')]
        RightBracket,
        [Token(@"\[", '[')]
        LeftSquare,
        [Token(@"\]", ']')]
        RightSquare,
        [Token(@"\{", '{')]
        StartBlock,
        [Token(@"\}", '}')]
        EndBlock,
        [Token(@",", ',')]
        Seperator,
        [Token(";", ';')]
        EndStatement,
        [Token(@"""([^""\\]|(\\"")|(\\u[\dabcdefABCDEF]{4}))*""", 'N')]
        String,
        [Token(@"'([^'\\]|((\\u[\dabcdefABCDEF]{4})|(\\('|\\))))'", 'N')]
        Char,
        [Token(@"\d+", 'N')]
        Number,
        [Token(@"0x[\dabcdefABCDEF]+", 'N')]
        HexNumber,
        [Token(Tokens.Operator, 'O')]
        Operator,
        [Token(Tokens.Identifier, 'I')]
        Identifier
    }
}