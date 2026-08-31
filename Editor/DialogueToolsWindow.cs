#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using EEsto.DialogueNyaa;
using UnityEditor;
using UnityEngine;

namespace EEsto.EditorTools
{
    public class DialogueToolsWindow : EditorWindow
    {
        private Object target;
        private bool merge = true;
        private string mergedName = "AllDialogues";
        private string langSuffix = "EN";
        private string saveDirectory = "Assets/Resources/Languages/EN";

        private Vector2 scroll;
        private string dumpText = "";

        [MenuItem("Tools/EEsto Tools/DialogueNyaa Parser")]
        public static void ShowWindow() => GetWindow<DialogueToolsWindow>("Dialogue Tools");

        private void OnGUI()
        {
            target = EditorGUILayout.ObjectField("Archivo o Carpeta", target, typeof(Object), false);
            if (target == null) return;

            string assetPath = AssetDatabase.GetAssetPath(target);
            bool isFolder = AssetDatabase.IsValidFolder(assetPath);

            EditorGUILayout.Space();

            if (!isFolder && target is TextAsset ta)
            {
                if (GUILayout.Button("Dump (ver programa compilado)", GUILayout.Height(24)))
                    dumpText = DialogueParser.Parse(ta.name, ta.text).Dump();

                if (!string.IsNullOrEmpty(dumpText))
                {
                    if (GUILayout.Button("Guardar dump a .log"))
                    {
                        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", $"{ta.name}.dump.log"));
                        File.WriteAllText(path, dumpText);
                        Debug.Log($"[Dialogue] Dump en {path}");
                    }

                    scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(320));
                    EditorGUILayout.TextArea(dumpText, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Listar hablantes"))
            {
                List<string> speakers = DialogueLocExporter.CollectSpeakers(assetPath, isFolder);
                Debug.Log($"[Dialogue] {speakers.Count} hablantes:\n" + string.Join("\n", speakers));
            }

            EditorGUILayout.Space();
            GUILayout.Label("Localización", EditorStyles.boldLabel);

            langSuffix = EditorGUILayout.TextField("Sufijo idioma", langSuffix);

            if (isFolder)
            {
                merge = EditorGUILayout.Toggle("Unir en un solo JSON", merge);
                if (merge) mergedName = EditorGUILayout.TextField("Nombre del archivo", mergedName);
            }

            EditorGUILayout.BeginHorizontal();
            saveDirectory = EditorGUILayout.TextField("Destino", saveDirectory);
            if (GUILayout.Button("Buscar", GUILayout.Width(60)))
            {
                string sel = EditorUtility.OpenFolderPanel("Carpeta de destino", Application.dataPath, "");
                if (!string.IsNullOrEmpty(sel) && sel.StartsWith(Application.dataPath))
                    saveDirectory = "Assets" + sel.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Extraer a JSON", GUILayout.Height(28)))
            {
                string dir = GetAbsoluteProjectPath(saveDirectory);

                if (isFolder)
                    DialogueLocExporter.ExportFolder(assetPath, merge, dir, mergedName, langSuffix);
                else if (target is TextAsset t)
                    DialogueLocExporter.ExportSingle(t, Path.Combine(dir, $"{t.name}_{langSuffix}.json"));

                AssetDatabase.Refresh();
                Debug.Log($"[Localization] Exportación completada en {saveDirectory}");
            }
        }
        
        private static string GetAbsoluteProjectPath(
            string projectRelativePath)
        {
            string projectRoot =
                Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        ".."));

            return Path.GetFullPath(
                Path.Combine(
                    projectRoot,
                    projectRelativePath));
        }
    }

    public static class DialogueContextMenu
    {
        [MenuItem("Assets/Dialogue/Dump", true)]
        private static bool Validate() => Selection.activeObject is TextAsset;

        [MenuItem("Assets/Dialogue/Dump")]
        private static void Dump()
        {
            var asset = (TextAsset)Selection.activeObject;
            Debug.Log(DialogueParser.Parse(asset.name, asset.text).Dump());
        }
    }
}
#endif