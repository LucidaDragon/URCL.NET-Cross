using System;
using System.Linq;

namespace URCL.NET
{
    public class ConfigurationBuilder
    {
        public Configuration Configuration { get; set; } = new Configuration();

        public void Configure(string configLine, Action<string> errorOut)
        {
            var configArgs = configLine.Split(' ');

            if (configArgs.Length == 0) return;

            bool found = false;
            foreach (var prop in typeof(Configuration).GetProperties())
            {
                if (prop.CanWrite && prop.Name.ToLower() == configArgs[0].ToLower())
                {
                    found = true;

                    if (prop.PropertyType == typeof(bool))
                    {
                        prop.SetValue(Configuration, true);
                    }
                    else if (configArgs.Length < 2)
                    {
                        errorOut($"Configuration \"{prop.Name}\" requires a value.");
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        if (int.TryParse(configArgs[1], out int value))
                        {
                            prop.SetValue(Configuration, value);
                        }
                        else
                        {
                            errorOut($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                        }
                    }
                    else if (prop.PropertyType == typeof(long))
                    {
                        if (long.TryParse(configArgs[1], out long value))
                        {
                            prop.SetValue(Configuration, value);
                        }
                        else
                        {
                            errorOut($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                        }
                    }
                    else if (prop.PropertyType == typeof(ushort))
                    {
                        if (ushort.TryParse(configArgs[1], out ushort value))
                        {
                            prop.SetValue(Configuration, value);
                        }
                        else
                        {
                            errorOut($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                        }
                    }
                    else if (prop.PropertyType == typeof(ulong))
                    {
                        if (ulong.TryParse(configArgs[1], out ulong value))
                        {
                            prop.SetValue(Configuration, value);
                        }
                        else
                        {
                            errorOut($"Value \"{configArgs[1]}\" is not valid for configuration \"{prop.Name}\".");
                        }
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(Configuration, string.Join(" ", configArgs.Skip(1)));
                    }
                    else
                    {
                        errorOut($"Configuration \"{prop.Name}\" is not supported.");
                    }
                }
            }

            if (!found) errorOut($"Configuration \"{configArgs[0]}\" is not valid.");
        }
    }
}
