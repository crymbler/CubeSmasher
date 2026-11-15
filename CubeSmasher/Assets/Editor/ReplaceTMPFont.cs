using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ReplaceTMPFont
{
    const string OldFontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";
    const string NewFontPath = "Assets/References/Rubik-VariableFont_wght SDF.asset";

    // --- стиль ---
    const bool ApplyCentering = true;          // центр по горизонтали + middle по вертикали
    const float MinFontSize = 10f;             // нижний предел автосайза
    // -------------

    static readonly string[] SkipFolders = { "NativeVoxReaderForUnity" };

    [MenuItem("Tools/Replace TMP Font")]
    static void Replace()
    {
        var oldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldFontPath);
        var newFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NewFontPath);
        if (oldFont == null || newFont == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Не найден старый или новый Font Asset. Проверь пути:\n" + OldFontPath + "\n" + NewFontPath, "OK");
            return;
        }

        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int changedPrefabs = 0, changedScenes = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsSkipped(path)) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = false;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (ApplyTo(text, oldFont, newFont)) { dirty = true; changedPrefabs++; }
            }
            if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsSkipped(path)) continue;

            try
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool dirty = false;
                foreach (var text in Object.FindObjectsOfType<TMP_Text>(true))
                {
                    if (ApplyTo(text, oldFont, newFont)) { dirty = true; changedScenes++; }
                }
                if (dirty) EditorSceneManager.SaveScene(scene);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Не удалось обработать сцену " + path + ": " + e.Message);
            }
        }

        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        EditorUtility.DisplayDialog("Готово",
            "Префабы: " + changedPrefabs + "\nСцены: " + changedScenes, "OK");
    }

    static bool ApplyTo(TMP_Text text, TMP_FontAsset oldFont, TMP_FontAsset newFont)
    {
        bool dirty = false;

        if (text.font == oldFont) { text.font = newFont; dirty = true; }

        if (ApplyCentering && text.alignment != TextAlignmentOptions.Center)
        {
            text.alignment = TextAlignmentOptions.Center;
            dirty = true;
        }

        // максимум автосайза = текущий размер шрифта
        float max = text.fontSize;
        if (max <= 1f) max = 36f;                      // страховка от нулевого/сбитого размера
        float min = Mathf.Min(MinFontSize, max - 1f);  // min строго меньше max

        if (!text.enableAutoSizing) dirty = true;
        if (!Mathf.Approximately(text.fontSizeMin, min)) dirty = true;
        if (!Mathf.Approximately(text.fontSizeMax, max)) dirty = true;

        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;

        return dirty;
    }

    static bool IsSkipped(string path)
    {
        foreach (var f in SkipFolders)
            if (path.Contains(f)) return true;
        return false;
    }
}