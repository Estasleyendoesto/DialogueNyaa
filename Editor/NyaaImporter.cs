#if UNITY_EDITOR

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace EEsto.EditorTools
{
    [ScriptedImporter(2, "nyaa")]
    public sealed class NyaaImporter : ScriptedImporter
    {
        public override void OnImportAsset(
            AssetImportContext context)
        {
            string text =
                File.ReadAllText(context.assetPath);

            string assetName =
                Path.GetFileNameWithoutExtension(
                    context.assetPath);

            var textAsset = new TextAsset(text)
            {
                name = assetName
            };

            context.AddObjectToAsset(
                "main",
                textAsset);

            context.SetMainObject(textAsset);
        }
    }
}

#endif