using System;
using System.Collections.Generic;
using System.Linq;

namespace LuC
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TokenAttribute : Attribute
    {
        public string Regex { get; }
        public char Id { get; }
        public int Layer { get; }
        public Dictionary<string, int> Attributes { get; }

        public TokenAttribute(string regex, char id, int layer = -1, params string[] attributes)
        {
            Regex = regex;
            Id = id;
            Layer = layer;

            if (attributes.Length > 0)
            {
                Attributes = attributes.Select(attrib =>
                {
                    var values = attrib.Split(':');
                    return new KeyValuePair<string, int>(values[0].Trim(), int.Parse(values[1].Trim()));
                }).ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            else
            {
                Attributes = new Dictionary<string, int>();
            }
        }
    }
}
