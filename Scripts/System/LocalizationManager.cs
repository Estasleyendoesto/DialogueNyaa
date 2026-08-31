using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EEsto.DialogueNyaa
{
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _translations = new(StringComparer.Ordinal);

        public static event Action LanguageChanged;

        public static string CurrentLanguage { get; private set; }

        public static int TranslationCount => _translations.Count;

        public static void LoadLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                Debug.LogWarning(
                    "[Localization] El código de idioma está vacío.");

                return;
            }

            string normalizedCode = languageCode.Trim().ToUpperInvariant();
            string resourcesPath = $"Languages/{normalizedCode}";
            
            TextAsset[] files = Resources.LoadAll<TextAsset>(resourcesPath);

            Array.Sort(
                files,
                (left, right) => string.Compare(
                    left.name,
                    right.name,
                    StringComparison.Ordinal));

            var loadedTranslations = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (TextAsset file in files) LoadFile(file, loadedTranslations);

            _translations = loadedTranslations;
            CurrentLanguage = normalizedCode;

            if (files.Length == 0)
            {
                Debug.LogWarning(
                    $"[Localization] No se encontraron archivos en " +
                    $"'Resources/{resourcesPath}'.");
            }
            else
            {
                Debug.Log(
                    $"[Localization] {normalizedCode} cargado: " +
                    $"{_translations.Count} traducciones desde " +
                    $"{files.Length} archivo(s).");
            }

            LanguageChanged?.Invoke();
        }

        public static string Get(string localizationKey, string originalText)
        {
            if (string.IsNullOrEmpty(localizationKey))
                return originalText ?? string.Empty;

            return _translations.TryGetValue(
                localizationKey,
                out string translation)
                    ? translation
                    : originalText ?? string.Empty;
        }

        public static bool TryGet(string localizationKey, out string translation)
        {
            if (string.IsNullOrEmpty(localizationKey))
            {
                translation = null;
                return false;
            }

            return _translations.TryGetValue(localizationKey, out translation);
        }

        public static void Clear()
        {
            _translations = new Dictionary<string, string>(StringComparer.Ordinal);

            CurrentLanguage = null;
            LanguageChanged?.Invoke();
        }

        private static void LoadFile(TextAsset file, Dictionary<string, string> translations)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.text))
                return;

            Dictionary<string, string> fileTranslations;

            try
            {
                fileTranslations =
                    JsonConvert.DeserializeObject<
                        Dictionary<string, string>>(file.text);
            }
            catch (JsonException exception)
            {
                Debug.LogError(
                    $"[Localization] JSON inválido en " +
                    $"'{file.name}': {exception.Message}");

                return;
            }

            if (fileTranslations == null) return;

            foreach (KeyValuePair<string, string> entry in fileTranslations)
            {
                if (string.IsNullOrEmpty(entry.Key))
                    continue;

                if (translations.ContainsKey(entry.Key))
                {
                    Debug.LogWarning(
                        $"[Localization] La clave '{entry.Key}' " +
                        $"aparece repetida. Se usará el valor de " +
                        $"'{file.name}'.");
                }

                translations[entry.Key] =
                    entry.Value ?? string.Empty;
            }
        }
    }
}