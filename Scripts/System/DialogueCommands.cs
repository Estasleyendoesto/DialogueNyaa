using System;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    internal static class DialogueCommands
    {
        public static void RegisterDefaults(
            Action<object[]> onReturnCallback, 
            Action<string, string[]> onEventCallback)
        {
            DialogueContext.Commands["return"] = args => ExecuteReturn(args, onReturnCallback);
            DialogueContext.Commands["event"]  = args => ExecuteEvent(args, onEventCallback);
            DialogueContext.Commands["log"]    = ExecuteLog;
            DialogueContext.Commands["warn"]   = ExecuteWarn;
        }

        public static void UnregisterDefaults()
        {
            DialogueContext.Commands.Remove("return");
            DialogueContext.Commands.Remove("event");
            DialogueContext.Commands.Remove("log");
            DialogueContext.Commands.Remove("warn");
        }

        private static void ExecuteReturn(string[] args, Action<object[]> onReturnCallback)
        {
            if (args == null || args.Length == 0)
            {
                Debug.LogWarning("[Dialogue] Return sin argumentos.");
                return;
            }

            object[] parsedArgs = CommandParser.ParseArgs(args);
            onReturnCallback?.Invoke(parsedArgs);
        }
        
        private static void ExecuteEvent(string[] args, Action<string, string[]> onEvent)
        {
            if (args == null || args.Length == 0)
            {
                Debug.LogWarning("[DialogueNyaa] Comando 'event' requiere al menos un nombre de evento.");
                return;
            }

            string eventName = args[0];
            string[] eventArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

            onEvent?.Invoke(eventName, eventArgs);
        }
        
        private static void ExecuteLog(string[] args)
        {
            if (args == null || args.Length == 0) return;
            Debug.Log($"[DialogueNyaa] Log: {string.Join(" ", args)}");
        }
        
        private static void ExecuteWarn(string[] args)
        {
            if (args == null || args.Length == 0) return;
            Debug.LogWarning($"[DialogueNyaa] Warn: {string.Join(" ", args)}");
        }
    }
}