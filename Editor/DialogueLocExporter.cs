#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EEsto.DialogueNyaa;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace EEsto.EditorTools
{
    public static class DialogueLocExporter
    {
        public static void Collect(
            TextAsset file,
            Dictionary<string, string> translations)
        {
            if (file == null || translations == null)
                return;

            DialogueProgram program =
                DialogueParser.Parse(
                    file.name,
                    file.text);

            foreach (Instruction instruction in program.Code)
            {
                switch (instruction.Op)
                {
                    case OpCode.Say:
                        CollectLine(
                            instruction.Line,
                            translations,
                            file.name);

                        break;

                    case OpCode.Choice:
                        CollectChoices(
                            instruction.Options,
                            translations,
                            file.name);

                        break;
                }
            }
        }

        public static List<string> CollectSpeakers(
            string assetPath,
            bool isFolder)
        {
            var speakers = new SortedSet<string>(
                StringComparer.OrdinalIgnoreCase);

            string[] guids = isFolder
                ? AssetDatabase.FindAssets(
                    "t:TextAsset",
                    new[] { assetPath })
                : new[]
                {
                    AssetDatabase.AssetPathToGUID(assetPath)
                };

            foreach (string guid in guids)
            {
                TextAsset file = Load(guid);

                if (file == null)
                    continue;

                DialogueProgram program =
                    DialogueParser.Parse(
                        file.name,
                        file.text);

                foreach (Instruction instruction in program.Code)
                {
                    if (instruction.Op != OpCode.Say ||
                        instruction.Line == null ||
                        string.IsNullOrEmpty(
                            instruction.Line.Speaker))
                    {
                        continue;
                    }

                    speakers.Add(
                        instruction.Line.Speaker);
                }
            }

            return new List<string>(speakers);
        }

        public static void ExportSingle(
            TextAsset file,
            string savePath)
        {
            if (file == null)
                return;

            var translations =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            Collect(file, translations);
            Save(translations, savePath);
        }

        public static void ExportFolder(
            string folderPath,
            bool merge,
            string saveDirectory,
            string mergedName,
            string suffix)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:TextAsset",
                    new[] { folderPath });

            if (merge)
            {
                var translations =
                    new Dictionary<string, string>(
                        StringComparer.Ordinal);

                foreach (string guid in guids)
                    Collect(Load(guid), translations);

                Save(
                    translations,
                    Path.Combine(
                        saveDirectory,
                        $"{mergedName}.json"));

                return;
            }

            foreach (string guid in guids)
            {
                TextAsset file = Load(guid);

                if (file == null)
                    continue;

                var translations =
                    new Dictionary<string, string>(
                        StringComparer.Ordinal);

                Collect(file, translations);

                Save(
                    translations,
                    Path.Combine(
                        saveDirectory,
                        $"{file.name}_{suffix}.json"));
            }
        }

        private static void CollectLine(
            DialogueLine line,
            Dictionary<string, string> translations,
            string source)
        {
            if (line == null)
                return;

            Add(
                translations,
                line.LocKey,
                line.Text,
                source);

            if (!string.IsNullOrEmpty(line.SpeakerKey))
            {
                AddIfMissing(
                    translations,
                    line.SpeakerKey,
                    line.Speaker);
            }
        }

        private static void CollectChoices(
            ChoiceOption[] options,
            Dictionary<string, string> translations,
            string source)
        {
            if (options == null)
                return;

            foreach (ChoiceOption option in options)
            {
                if (option == null)
                    continue;

                Add(
                    translations,
                    option.LocKey,
                    option.Text,
                    source);
            }
        }

        private static TextAsset Load(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            string assetPath =
                AssetDatabase.GUIDToAssetPath(guid);

            return AssetDatabase.LoadAssetAtPath<TextAsset>(
                assetPath);
        }

        private static void Add(
            Dictionary<string, string> translations,
            string key,
            string text,
            string source)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (translations.TryAdd(
                    key,
                    text ?? string.Empty))
            {
                return;
            }

            Debug.LogWarning(
                $"[Localization] Clave duplicada omitida: " +
                $"'{key}' en '{source}'.");
        }

        private static void AddIfMissing(
            Dictionary<string, string> translations,
            string key,
            string text)
        {
            if (string.IsNullOrEmpty(key))
                return;

            translations.TryAdd(
                key,
                text ?? string.Empty);
        }

        private static void Save(
            Dictionary<string, string> translations,
            string absolutePath)
        {
            string directory =
                Path.GetDirectoryName(absolutePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var sortedTranslations =
                new SortedDictionary<string, string>(
                    translations,
                    StringComparer.Ordinal);

            string json = JsonConvert.SerializeObject(
                sortedTranslations,
                Formatting.Indented);

            File.WriteAllText(
                absolutePath,
                json,
                new UTF8Encoding(false));
        }
    }
}

#endif