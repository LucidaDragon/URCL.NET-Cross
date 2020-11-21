namespace LuC
{
    public enum MetaToken
    {
        Invalid,
        [Token(@"n (I)", '\0', 0, Tokens.NameAttribute + "1")]
        NamespaceDeclaration,
        [Token(@"o (I) (O) ?\(( ?I I ?(, ?I I ?)?)\)", '\0', 1, Tokens.TypeAttribute + "1", Tokens.NameAttribute + "2", Tokens.ParametersAttribute + "3")]
        OperatorDeclaration,
        [Token(@"(I) (I) ?\(( ?(I I ?)?(, ?I I ?)*)\)", '\0', 1, Tokens.TypeAttribute + "1", Tokens.NameAttribute + "2", Tokens.ParametersAttribute + "3")]
        FunctionDeclaration,
        [Token("w", 'w')]
        While,
        [Token("f", 'f')]
        If,
        [Token("e", 'e')]
        Else,
        [Token("r", 'r')]
        Return,
        [Token("(I) (I)", 'L', -1, Tokens.TypeAttribute + "1", Tokens.NameAttribute + "2")]
        LocalDeclaration,
        [Token(Tokens.MatchBraces, 'B')]
        Block,
        [Token(@"(I) ?(" + Tokens.MatchBrackets + ")", 'C', -1, Tokens.NameAttribute + "1")]
        Call,
        Argument,
        [Token(Tokens.MatchBrackets, 'E')]
        SubExpression,
        [Token(@"(O)", 'O', -1, Tokens.NameAttribute + "1")]
        Operator,
        [Token(@"(I)", 'I', -1, Tokens.NameAttribute + "1")]
        IdentifierExpression,
        [Token(@"(N)", 'E', -1, Tokens.ValueAttribute + "1")]
        LiteralExpression,
        [Token(@" ", ' ')]
        Whitespace,
        [Token(@";", ';')]
        EndStatement
    }
}
