using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LuC.Tree;

namespace LuC
{
    public static class Parser
    {
        public static IEnumerable<Namespace> Parse(Compiler compiler, string src)
        {
            return ParseNamespaces(compiler, LexMeta(src, Lex(src)));
        }

        public static IEnumerable<Namespace> ParseNamespaces(Compiler compiler, ArraySegment<TokenData<MetaToken>> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Type)
                {
                    case MetaToken.NamespaceDeclaration:
                        if (token.Attributes.TryGetValue(Tokens.Name, out string name))
                        {
                            yield return new Namespace(token.GetSourceIndex(), token.GetSourceLength(), name, ParseFunctions(compiler, GetChildBlock(tokens, i, out int blockIndex).LogicalChildren));
                            i = blockIndex;
                        }
                        else
                        {
                            throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                        }
                        break;
                    case MetaToken.Whitespace:
                        break;
                    default:
                        throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.WrongContext);
                }
            }
        }

        public static IEnumerable<Function> ParseFunctions(Compiler compiler, ArraySegment<TokenData<MetaToken>> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Type)
                {
                    case MetaToken.FunctionDeclaration:
                        if (token.Attributes.TryGetValue(Tokens.Name, out string name) &&
                            token.Attributes.TryGetValue(Tokens.Type, out string returnType) &&
                            token.Attributes.TryGetValue(Tokens.Parameters, out string parameters))
                        {
                            yield return new Function(token.GetSourceIndex(), token.GetSourceLength(), name, returnType, ParseFunctionParameters(compiler, token, parameters), ParseCodeBody(compiler, GetChildBlock(tokens, i, out int blockIndex).LogicalChildren));
                            i = blockIndex;
                        }
                        else
                        {
                            throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                        }
                        break;
                    case MetaToken.Whitespace:
                        break;
                    default:
                        throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.WrongContext);
                }
            }
        }

        public static IEnumerable<Field> ParseFunctionParameters(Compiler compiler, TokenData<MetaToken> target, string parameters)
        {
            if (parameters.Trim().Length == 0) return new Field[0];

            return parameters.Split(',').Select(param =>
            {
                var parts = Regex.Split(param.Trim(), @"\s+");

                if (parts.Length == 2)
                {
                    return new Field(target.GetSourceIndex(), target.GetSourceLength(), compiler, parts[0], parts[1]);
                }
                else
                {
                    throw new SourceError(target.GetSourceIndex(), target.GetSourceLength(), SourceError.InvalidParameterSyntax);
                }
            });
        }

        public static IEnumerable<Statement> ParseCodeBody(Compiler compiler, ArraySegment<TokenData<MetaToken>> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Type)
                {
                    case MetaToken.While:
                        {
                            var condition = GetChildBlock(tokens, i, out int conditionIndex, MetaToken.SubExpression);
                            yield return new WhileStatement(token.GetSourceIndex(), token.GetSourceLength(), ParseExpression(condition, condition.LogicalChildren), ParseCodeBody(compiler, GetChildBlock(tokens, conditionIndex, out int blockIndex).LogicalChildren));
                            i = blockIndex;
                        }
                        break;
                    case MetaToken.If:
                        {
                            var condition = GetChildBlock(tokens, i, out int conditionIndex, MetaToken.SubExpression);
                            var trueBlock = GetChildBlock(tokens, conditionIndex, out int blockIndex);
                            var elseKeyword = GetChildBlock(tokens, blockIndex, out int elseStart, MetaToken.Else, false, false);

                            if (elseKeyword.Type == MetaToken.Invalid)
                            {
                                yield return new IfStatement(token.GetSourceIndex(), token.GetSourceLength(), ParseExpression(condition, condition.LogicalChildren), ParseCodeBody(compiler, trueBlock.LogicalChildren), new Statement[0]);
                            }
                            else
                            {
                                var falseBlock = GetChildBlock(tokens, elseStart, out blockIndex);

                                yield return new IfStatement(token.GetSourceIndex(), token.GetSourceLength(), ParseExpression(condition, condition.LogicalChildren), ParseCodeBody(compiler, trueBlock.LogicalChildren), ParseCodeBody(compiler, falseBlock.LogicalChildren));
                            }

                            i = blockIndex;
                        }
                        break;
                    case MetaToken.Return:
                        {
                            GetChildBlock(tokens, i, out int endIndex, MetaToken.EndStatement, true);
                            var offset = 1;
                            while (tokens[i + offset].Type == MetaToken.Whitespace) offset++;
                            var resultTokens = tokens.Slice(i + offset, endIndex - (i + offset));
                            yield return new ReturnStatement(token.GetSourceIndex(), token.GetSourceLength(), resultTokens.Count == 0 ? null : ParseExpression(token, resultTokens));
                            i = endIndex;
                        }
                        break;
                    case MetaToken.LocalDeclaration:
                        {
                            if (token.Attributes.TryGetValue(Tokens.Type, out string type) &&
                                token.Attributes.TryGetValue(Tokens.Name, out string name))
                            {
                                GetChildBlock(tokens, i, out int endIndex, MetaToken.EndStatement);
                                yield return new LocalDeclarationStatement(token.GetSourceIndex(), token.GetSourceLength(), new Field(token.GetSourceIndex(), token.GetSourceLength(), new UnresolvedDataType(token.GetSourceIndex(), token.GetSourceLength(), compiler, type), name));
                                i = endIndex;
                            }
                            else
                            {
                                throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                            }
                        }
                        break;
                    case MetaToken.Call:
                        {
                            GetChildBlock(tokens, i, out int endIndex, MetaToken.EndStatement);
                            yield return new ExpressionStatement(token.GetSourceIndex(), token.GetSourceLength(), ParseExpression(token, new[] { token }));
                            i = endIndex;
                        }
                        break;
                    case MetaToken.EndStatement:
                        throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.UnexpectedEnd);
                    case MetaToken.Whitespace:
                        break;
                    default:
                        throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.WrongContext);
                }
            }
        }

        public static Expression ParseExpression(TokenData<MetaToken> target, ArraySegment<TokenData<MetaToken>> tokens)
        {
            if (tokens.Count == 0)
            {
                throw new SourceError(target.GetSourceIndex(), target.GetSourceLength(), SourceError.EmptyExpression);
            }
            else if (tokens.Count == 1)
            {
                var token = tokens[0];

                switch (token.Type)
                {
                    case MetaToken.SubExpression:
                        return ParseExpression(token, token.LogicalChildren);
                    case MetaToken.IdentifierExpression:
                        {
                            if (token.Attributes.TryGetValue(Tokens.Name, out string name))
                            {
                                return new IdentifierExpression(token.GetSourceIndex(), token.GetSourceLength(), name);
                            }
                            else
                            {
                                throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                            }
                        }
                    case MetaToken.LiteralExpression:
                        {
                            if (token.Attributes.TryGetValue(Tokens.Value, out string value))
                            {
                                return new LiteralExpression(token.GetSourceIndex(), token.GetSourceLength(), Compiler.NativeInt, value);
                            }
                            else
                            {
                                throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                            }
                        }
                    case MetaToken.Call:
                        {
                            if (token.Attributes.TryGetValue(Tokens.Name, out string name))
                            {
                                return new CallExpression(token.GetSourceIndex(), token.GetSourceLength(), name, token.LogicalChildren.Value.Select(child => ParseExpression(child, child.LogicalChildren)));
                            }
                            else
                            {
                                throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                            }
                        }
                    default:
                        throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.WrongContext);
                }
            }
            else
            {
                var postfix = new List<TokenData<MetaToken>>();
                var stack = new Stack<TokenData<MetaToken>>();

                for (int i = 0; i < tokens.Count; i++)
                {
                    var token = tokens[i];

                    switch (token.Type)
                    {
                        case MetaToken.SubExpression:
                            postfix.Add(token);
                            break;
                        case MetaToken.Operator:
                            stack.Push(token);
                            break;
                        case MetaToken.IdentifierExpression:
                            postfix.Add(token);
                            break;
                        case MetaToken.LiteralExpression:
                            postfix.Add(token);
                            break;
                        case MetaToken.Whitespace:
                            break;
                        default:
                            throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.WrongContext);
                    }
                }

                while (stack.Count > 0) postfix.Add(stack.Pop());

                if (postfix.Count == 3)
                {
                    var left = ParseExpression(postfix[0], new ArraySegment<TokenData<MetaToken>>(new[] { postfix[0] }));
                    var right = ParseExpression(postfix[1], new ArraySegment<TokenData<MetaToken>>(new[] { postfix[1] }));
                    var token = postfix[2];

                    if (token.Type == MetaToken.Operator)
                    {
                        if (token.Attributes.TryGetValue(Tokens.Name, out string name))
                        {
                            var op = new FunctionReference(target.GetSourceIndex(), target.GetSourceLength(), name, new[] { left.ReturnType, right.ReturnType });

                            return new BinaryExpression(target.GetSourceIndex(), target.GetSourceLength(), left, op, right);
                        }
                        else
                        {
                            throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.MissingAttribute);
                        }
                    }
                    else
                    {
                        throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.InvalidExpression);
                    }
                }

                throw new SourceError(target.GetSourceIndex(), target.GetSourceLength(), SourceError.InvalidExpression);
            }
        }

        public static ArraySegment<TokenData<MetaToken>> LexMeta<T>(string src, ArraySegment<TokenData<T>> tokens, int layer = 0) where T : Enum
        {
            var buffer = new List<TokenData<MetaToken>>();
            var generic = tokens.Select(t => (TokenData<Enum>)t).ToArray();
            var map = generic.ToTokenIdString();

            for (int i = 0; i < map.Length;)
            {
                var length = Tokens.MatchToken(map, i, out MetaToken token);

                if (length > 0)
                {
                    if (Tokens.GetLayer(token) <= layer)
                    {
                        var segment = new ArraySegment<TokenData<Enum>>(generic, i, length);
                        var data = new TokenData<MetaToken>(token, i, length, segment);

                        if ((data.Type == MetaToken.Block || data.Type == MetaToken.SubExpression) && segment.Count > 2)
                        {
                            data.LogicalChildren = LexMeta(src, segment.Slice(1, segment.Count - 2), layer + 1);
                        }
                        else if (data.Type == MetaToken.Call)
                        {
                            data.LogicalChildren = LexArguments(src, data, segment.Slice(1, segment.Count - 1), layer + 1);
                        }

                        data.ApplyAttributes(generic, map, src);

                        buffer.Add(data);
                    }
                    else
                    {
                        var errorSource = tokens[i];
                        throw new SourceError(errorSource.GetSourceIndex(), errorSource.GetSourceLength(), SourceError.WrongContext);
                    }
                }
                else
                {
                    var errorSource = tokens[i];
                    throw new SourceError(errorSource.GetSourceIndex(), errorSource.GetSourceLength(), SourceError.InvalidSyntax);
                }

                i += length;
            }

            return buffer.ToArray();
        }

        private static ArraySegment<TokenData<MetaToken>> LexArguments<T>(string src, TokenData<MetaToken> context, ArraySegment<TokenData<T>> tokens, int layer = 0) where T : Enum
        {
            var parts = new List<ArraySegment<TokenData<T>>>();
            var start = 0;
            var depth = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (!(token.Type is Token type))
                {
                    throw new SourceError(token.GetSourceIndex(), token.GetSourceLength(), SourceError.WrongContext);
                }

                if (type == Token.LeftBracket)
                {
                    depth++;

                    if (depth == 1)
                    {
                        start = i + 1;
                    }
                }
                else if (type == Token.RightBracket)
                {
                    if (depth == 1)
                    {
                        parts.Add(tokens.Slice(start, i - start));
                        start = i + 1;
                    }

                    depth--;
                }
                else if (type == Token.Seperator && depth == 1)
                {
                    parts.Add(tokens.Slice(start, i - start));
                    start = i + 1;
                }
            }

            if (depth != 0) throw new SourceError(tokens.First().GetSourceIndex(), tokens.First().GetSourceLength(), SourceError.InvalidSyntax);

            return parts.Select(part => new TokenData<MetaToken>(MetaToken.Argument, context.Start, context.Length, tokens.Select(t => (TokenData<Enum>)t).ToArray(), Trim(LexMeta(src, part, layer)))).ToArray();
        }

        public static ArraySegment<TokenData<Token>> Lex(string src)
        {
            var buffer = new List<TokenData<Token>>();

            for (int i = 0; i < src.Length;)
            {
                int length = Tokens.MatchToken(src, i, out Token token);

                if (length == 0)
                {
                    throw new SourceError(i, 1, SourceError.InvalidSyntax);
                }

                buffer.Add(new TokenData<Token>(token, i, length));

                i += length;
            }

            return buffer.ToArray();
        }

        private static ArraySegment<TokenData<MetaToken>> Trim(ArraySegment<TokenData<MetaToken>> tokens)
        {
            var start = 0;
            var length = 0;

            if (tokens.Count == 0) return tokens;

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type != MetaToken.Whitespace)
                {
                    start = i;
                    break;
                }
            }

            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                if (tokens[i].Type != MetaToken.Whitespace)
                {
                    length = (i - start) + 1;
                    break;
                }
            }

            return tokens.Slice(start, length);
        }

        private static TokenData<MetaToken> GetChildBlock(ArraySegment<TokenData<MetaToken>> tokens, int index, out int position, MetaToken type = MetaToken.Block, bool ignoreNonWhitespace = false, bool throwException = true)
        {
            for (int i = index + 1; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.Type != MetaToken.Whitespace)
                {
                    if (token.Type == type)
                    {
                        position = i;
                        return token;
                    }
                    else if (!ignoreNonWhitespace)
                    {
                        break;
                    }
                }
            }

            if (throwException) throw new SourceError(tokens[index].GetSourceIndex(), tokens[index].GetSourceLength(), SourceError.MissingBlock);
            position = -1;
            return new TokenData<MetaToken>(MetaToken.Invalid, 0, 0);
        }
    }
}
