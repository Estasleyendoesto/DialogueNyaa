using System;
using System.Collections.Generic;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public static class Nyaa
    {
        // ================= Control de Reproducción =================
        
        public static void Play(TextAsset storyFile, string startNode = "start", GameObject dialoguePrefab = null)
            => DialogueEvents.Play(storyFile, startNode, dialoguePrefab);

        public static void Stop()
            => DialogueEvents.Stop();

        public static void Return(object[] args)
            => DialogueEvents.Return(args);

        // ================= Eventos Globales =================
        
        public static event Action<object[]> OnReturn
        {
            add => DialogueEvents.OnReturn += value;
            remove => DialogueEvents.OnReturn -= value;
        }

        // ================= Condiciones y Predicados =================
        
        public static Dictionary<string, Func<bool>> Conditions 
            => DialogueContext.Conditions;

        public static Dictionary<string, Func<string[], bool>> Predicates 
            => DialogueContext.Predicates;

        public static Dictionary<string, Action<string[]>> Commands 
            => DialogueContext.Commands;

        public static void ClearContext() 
            => DialogueContext.Clear();

        // ================= Localización =================
        
        public static string CurrentLanguage => LocalizationManager.CurrentLanguage;

        public static void LoadLanguage(string languageCode) 
            => LocalizationManager.LoadLanguage(languageCode);

        public static string GetTranslation(string key, string fallbackText) 
            => LocalizationManager.Get(key, fallbackText);

        public static event Action LanguageChanged
        {
            add => LocalizationManager.LanguageChanged += value;
            remove => LocalizationManager.LanguageChanged -= value;
        }
    }
}