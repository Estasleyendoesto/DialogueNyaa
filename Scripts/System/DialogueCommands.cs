using System;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public sealed class DialogueCommands : MonoBehaviour
    {
        private Action<string[]> _returnCommand;

        private void Awake()
        {
            _returnCommand = ExecuteReturn;

            DialogueContext.Commands["return"] = _returnCommand;
        }

        private void OnDestroy()
        {
            if (DialogueContext.Commands.TryGetValue(
                    "return",
                    out Action<string[]> registeredCommand) &&
                registeredCommand == _returnCommand)
            {
                DialogueContext.Commands.Remove("return");
            }
        }

        private static void ExecuteReturn(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Debug.LogWarning(
                    "[Dialogue] Return sin argumentos.");

                return;
            }

            // Los valores de ':: Return' son identificadores (nombres de
            // elección), no datos tipados: se envían siempre como string.
            // Si se usara CommandParser.ParseArgs aquí, un valor como
            // "true"/"1" se convertiría en silencio a bool/int y rompería
            // cualquier receptor que compruebe "args[0] is not string".
            var stringArgs = new object[args.Length];

            for (int i = 0; i < args.Length; i++)
                stringArgs[i] = args[i];

            DialogueEvents.Return(stringArgs);
        }
    }
}