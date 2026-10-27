using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Install SafeArea Fitter
/// Ставит SafeAreaFitter на все объекты с именем SafeArea в префабах и в открытой сцене.
/// Контейнеры SafeArea уже есть в HUD и гараже, скрипта на них не было.
/// Повторный запуск второй компонент не добавляет.
/// </summary>
public static class SafeAreaInstaller
{
    [MenuItem("Tools/CubeSmasher/Install SafeArea Fitter")]
    public static void Install()
    {
        int prefabs = 0, scenes = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int added = InstallIn(root);
            if (added > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabs += added;
                Debug.Log("[SafeArea] " + path + ": добавлено " + added);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                scenes += InstallIn(root);
            }
            if (scenes > 0) EditorSceneManager.MarkSceneDirty(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SafeArea] Готово. В префабах: " + prefabs + ", в открытой сцене: " + scenes);
    }

    private static int InstallIn(GameObject root)
    {
        int added = 0;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "SafeArea") continue;
            if (t.GetComponent<SafeAreaFitter>() != null) continue;

            t.gameObject.AddComponent<SafeAreaFitter>();
            added++;
        }

        return added;
    }
}
