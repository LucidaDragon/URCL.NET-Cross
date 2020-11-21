using System;
using System.Collections.Generic;

namespace LuC
{
    public struct TokenData<T> where T : Enum
    {
        public T Type;
        public int Start;
        public int Length;
        public Reference<ArraySegment<TokenData<Enum>>> PhysicalChildren;
        public Reference<ArraySegment<TokenData<MetaToken>>> LogicalChildren;
        public Dictionary<string, string> Attributes;

        public TokenData(T token, int start, int length, ArraySegment<TokenData<Enum>>? physicalChildren = null, ArraySegment<TokenData<MetaToken>>? logicalChildren = null)
        {
            Type = token;
            Start = start;
            Length = length;

            if (physicalChildren is null)
            {
                PhysicalChildren = new ArraySegment<TokenData<Enum>>(new TokenData<Enum>[0]);
            }
            else
            {
                PhysicalChildren = physicalChildren.Value;
            }

            if (logicalChildren is null)
            {
                LogicalChildren = new ArraySegment<TokenData<MetaToken>>(new TokenData<MetaToken>[0]);
            }
            else
            {
                LogicalChildren = logicalChildren.Value;
            }

            Attributes = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            return PhysicalChildren.Value.Count > 0 ? $"{Type} {{{string.Join(", ", PhysicalChildren)}}}" : Type.ToString();
        }

        public static explicit operator TokenData<Enum>(TokenData<T> data)
        {
            return new TokenData<Enum>(data.Type, data.Start, data.Length, data.PhysicalChildren);
        }

        public string GetValue(string src)
        {
            return src.Substring(GetSourceIndex(), GetSourceLength());
        }

        public int GetSourceIndex()
        {
            if (PhysicalChildren.Value.Count > 0)
            {
                return PhysicalChildren.Value[0].GetSourceIndex();
            }
            else
            {
                return Start;
            }
        }

        public int GetSourceLength()
        {
            if (PhysicalChildren.Value.Count > 0)
            {
                var result = 0;

                foreach (var child in PhysicalChildren.Value)
                {
                    result += child.GetSourceLength();
                }

                return result;
            }
            else
            {
                return Length;
            }
        }
    }
}
