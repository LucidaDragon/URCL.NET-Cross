using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LuC
{
    public static class Tokens
    {
        public const string Identifier = @"\w[\w\d_]*(\.\w[\w\d_]*)*";
        public const string Operator = @"[^\w\d_\s\(\)\{\}\[\]""',;\.]+";
        public const string MatchBraces = @"\{((?>\{(?<c>)|[^{}]+|\}(?<-c>))*(?(c)(?!)))\}";
        public const string MatchBrackets = @"\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
        public const string MatchSquare = @"\[((?>\[(?<c>)|[^\[\]]+|\](?<-c>))*(?(c)(?!)))\]";

        public const string Name = nameof(Name);
        public const string NameAttribute = Name + ":";

        public const string Type = nameof(Type);
        public const string TypeAttribute = Type + ":";

        public const string Parameters = nameof(Parameters);
        public const string ParametersAttribute = Parameters + ":";

        public const string Value = nameof(Value);
        public const string ValueAttribute = Value + ":";

        public const string Left = nameof(Left);
        public const string LeftAttribute = Left + ":";

        public const string Right = nameof(Right);
        public const string RightAttribute = Right + ":";

        private static readonly Dictionary<Token, TokenAttribute> TokenAttributes = new Dictionary<Token, TokenAttribute>();
        private static readonly Dictionary<MetaToken, TokenAttribute> MetaTokenAttributes = new Dictionary<MetaToken, TokenAttribute>();

        static Tokens()
        {
            foreach (var token in typeof(Token).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (token.FieldType == typeof(Token))
                {
                    var attribs = token.GetCustomAttributes<TokenAttribute>();

                    if (attribs.Any())
                    {
                        TokenAttributes[(Token)token.GetValue(null)] = attribs.First();
                    }
                }
            }

            foreach (var token in typeof(MetaToken).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (token.FieldType == typeof(MetaToken))
                {
                    var attribs = token.GetCustomAttributes<TokenAttribute>();

                    if (attribs.Any())
                    {
                        MetaTokenAttributes[(MetaToken)token.GetValue(null)] = attribs.First();
                    }
                }
            }
        }

        public static int MatchToken(string src, int start, out Token token)
        {
            foreach (var attrib in TokenAttributes)
            {
                var match = Regex.Match(src.Substring(start), attrib.Value.Regex);

                if (match.Success && match.Index == 0)
                {
                    token = attrib.Key;
                    return match.Length;
                }
            }

            token = Token.Invalid;
            return 0;
        }

        public static int MatchToken(string src, int start, out MetaToken token)
        {
            foreach (var attrib in MetaTokenAttributes)
            {
                var match = Regex.Match(src.Substring(start), attrib.Value.Regex);

                if (match.Success && match.Index == 0)
                {
                    token = attrib.Key;
                    return match.Length;
                }
            }

            token = MetaToken.Invalid;
            return 0;
        }

        public static int GetLayer(MetaToken token)
        {
            if (MetaTokenAttributes.TryGetValue(token, out TokenAttribute attrib))
            {
                return attrib.Layer;
            }
            else
            {
                return -1;
            }
        }

        public static void ApplyAttributes<T>(this TokenData<T> data, ArraySegment<TokenData<Enum>> tokens, string map, string src) where T : Enum
        {
            if (!((
                    data.Type is Token value &&
                    TokenAttributes.TryGetValue(value, out TokenAttribute attrib)
                ) || (
                    data.Type is MetaToken metaValue &&
                    MetaTokenAttributes.TryGetValue(metaValue, out attrib)
                )))
            {
                return;
            }

            var match = Regex.Match(map.Substring(data.Start, data.Length), attrib.Regex);

            foreach (var pair in attrib.Attributes)
            {
                var group = match.Groups[pair.Value];
                
                if (group.Success)
                {
                    data.Attributes.Add(pair.Key, string.Concat(tokens.Slice(group.Index + data.Start, group.Length).Select(t => t.GetValue(src))));
                }
            }
        }

        public static string ToTokenIdString(this IEnumerable<TokenData<Enum>> tokens)
        {
            return ToTokenIdString(tokens.Select(t => t.Type));
        }

        public static string ToTokenIdString(this IEnumerable<Enum> tokens)
        {
            if (tokens.Any())
            {
                var first = tokens.First();

                if (first is Token)
                {
                    return ToTokenIdString(tokens.Cast<Token>());
                }
                else if (first is MetaToken)
                {
                    return ToTokenIdString(tokens.Cast<MetaToken>());
                }
                else
                {
                    throw new InvalidCastException();
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public static string ToTokenIdString(this IEnumerable<Token> tokens)
        {
            var buffer = new List<char>();

            foreach (var token in tokens)
            {
                char id = GetId(token);

                if (id != '\0')
                {
                    buffer.Add(id);
                }
            }

            return new string(buffer.ToArray());
        }

        public static string ToTokenIdString(this IEnumerable<MetaToken> tokens)
        {
            var buffer = new List<char>();

            foreach (var token in tokens)
            {
                char id = GetId(token);

                if (id != '\0')
                {
                    buffer.Add(id);
                }
            }

            return new string(buffer.ToArray());
        }

        public static char GetId(this Token token)
        {
            if (TokenAttributes.TryGetValue(token, out TokenAttribute attrib))
            {
                return attrib.Id;
            }
            else
            {
                return '\0';
            }
        }

        public static char GetId(this MetaToken token)
        {
            if (MetaTokenAttributes.TryGetValue(token, out TokenAttribute attrib))
            {
                return attrib.Id;
            }
            else
            {
                return '\0';
            }
        }
    }
}
