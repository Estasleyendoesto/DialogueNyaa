using System;
using System.Collections.Generic;

namespace EEsto.DialogueNyaa
{
    internal static class DialogueContext
    {
        // Condiciones directas
        public static readonly Dictionary<string, Func<bool>> Conditions =
            new(StringComparer.OrdinalIgnoreCase);
        
        // Condiciones con parámetros
        public static readonly Dictionary<string, Func<string[], bool>> Predicates =
            new(StringComparer.OrdinalIgnoreCase);

        public static readonly Dictionary<string, Action<string[]>> Commands =
            new(StringComparer.OrdinalIgnoreCase);

        
        public static bool EvaluateCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            condition = condition.Trim();

            bool invert = condition.StartsWith("!");
            if (invert)
            {
                condition = condition.Substring(1).Trim();
            }

            bool result = TryEvaluate(condition);
            return invert ? !result : result;
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
            Predicates.Clear();
            Commands.Clear();
        }
        
        
        //
        // Internal
        //
        
        private static bool TryEvaluate(string condition)
        {
            // Condición simple sin parámetros
            if (Conditions.TryGetValue(condition, out Func<bool> evaluateSimple))
                return evaluateSimple();

            // Condición con parámetros ("not_in kitchen", "has_item knife 2")
            return TryEvaluateParameterized(condition);
        }
        
        private static bool TryEvaluateParameterized(string rawCondition)
        {
            string[] parts = rawCondition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            string funcName = parts[0];
            if (!Predicates.TryGetValue(funcName, out Func<string[], bool> evaluateParam))
                return false;

            string[] args = new string[parts.Length - 1];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = parts[i + 1].Trim(' ', '"', '\'');
            }

            return evaluateParam(args);
        }
    }
}