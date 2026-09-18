using System;
using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public static class NyaaDialogue
    {
        // Control
        public static void Play(TextAsset file, string node = "start", GameObject prefab = null)
            => DialogueSystem.Instance?.Play(file, node, prefab);

        public static void Stop() => DialogueSystem.Instance?.Stop();
        public static void Pause() => DialogueSystem.Instance?.Pause();
        public static void Resume() => DialogueSystem.Instance?.Resume();
        public static void Continue() => DialogueSystem.Instance?.Continue();

        // Eventos
        public static event Action<string, string> OnStart
        {
            add { if (DialogueSystem.Instance) DialogueSystem.Instance.OnDialogueStarted += value; }
            remove { if (DialogueSystem.Instance) DialogueSystem.Instance.OnDialogueStarted -= value; }
        }

        public static event Action OnEnd
        {
            add { if (DialogueSystem.Instance) DialogueSystem.Instance.OnDialogueEnded += value; }
            remove { if (DialogueSystem.Instance) DialogueSystem.Instance.OnDialogueEnded -= value; }
        }

        public static event Action<object[]> OnReturn
        {
            add { if (DialogueSystem.Instance) DialogueSystem.Instance.OnReturn += value; }
            remove { if (DialogueSystem.Instance) DialogueSystem.Instance.OnReturn -= value; }
        }
        
        public static event Action<string, string[]> OnEvent
        {
            add { if (DialogueSystem.Instance) DialogueSystem.Instance.OnCustomEvent += value; }
            remove { if (DialogueSystem.Instance) DialogueSystem.Instance.OnCustomEvent -= value; }
        }

        // Contexto & Localización
        public static Dictionary<string, Func<bool>> Conditions => DialogueContext.Conditions;
        public static Dictionary<string, Func<string[], bool>> Predicates => DialogueContext.Predicates;
        public static Dictionary<string, Action<string[]>> Commands => DialogueContext.Commands;
        public static void ClearContext() => DialogueContext.Clear();

        public static string CurrentLanguage => LocalizationManager.CurrentLanguage;
        public static void LoadLanguage(string code) => LocalizationManager.LoadLanguage(code);
    }
}