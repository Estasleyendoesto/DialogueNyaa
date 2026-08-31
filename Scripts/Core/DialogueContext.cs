using System;
using System.Collections.Generic;

namespace EEsto.DialogueNyaa
{
    public static class DialogueContext
    {
        public static readonly Dictionary<string, Func<bool>> Conditions =
            new(StringComparer.OrdinalIgnoreCase);

        public static readonly Dictionary<string, Action<string[]>> Commands =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool EvaluateCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            return Conditions.TryGetValue(condition, out Func<bool> evaluate) && evaluate();
        }

        public static void ExecuteCommand(string command, string[] args)
        {
            if (Commands.TryGetValue(command, out Action<string[]> execute))
            {
                execute(args);
            }
        }

        public static void Clear()
        {
            Conditions.Clear();
            Commands.Clear();
        }
    }
}