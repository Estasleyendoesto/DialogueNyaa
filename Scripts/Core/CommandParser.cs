using System;
using System.Globalization;

namespace EEsto.DialogueNyaa
{
    public static class CommandParser
    {
        public static object[] ParseArgs(string[] args)
        {
            if (args == null || args.Length == 0)
                return Array.Empty<object>();

            var result = new object[args.Length];

            for (int i = 0; i < args.Length; i++)
                result[i] = ParseArgument(args[i]);

            return result;
        }

        public static object ParseArgument(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return raw;

            string value = Clean(raw);

            if (bool.TryParse(value, out bool boolean))
                return boolean;

            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int integer))
            {
                return integer;
            }

            if (float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float number))
            {
                return number;
            }

            return value;
        }

        public static bool TryFloat(
            string raw,
            out float value)
        {
            value = 0f;

            return !string.IsNullOrEmpty(raw) &&
                   float.TryParse(
                       Clean(raw),
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value);
        }

        private static string Clean(string raw)
        {
            return raw.Trim(' ', '"', '\'');
        }
    }
}