using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Setup Audio
/// Ставит ButtonClickSound на все Button и SliderTickSound на все Slider
/// в открытой сцене и во всех префабах. Повторный запуск не дублирует
/// компоненты. Клипы для кнопок и удара молота отдельным шагом не назначает —
/// это делается на самом Bootstrapper (поля _clickClip, _hammerHitClip, _unlockClip).
/// </summary>
public static class AudioSetupTool
{
    [MenuItem("Tools/CubeSmasher/Setup Audio")]
    public static void Setup()
    {
        int buttons = 0, sliders = 0;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                (int b, int s) = InstallIn(root);
                buttons += b;
                sliders += s;
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);

            (int b, int s) = InstallIn(prefab);
            if (b > 0 || s > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                buttons += b;
                sliders += s;
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Audio] Готово. Кнопок со звуком: " + buttons + ", слайдеров со звуком: " + sliders);
    }

    private static (int buttons, int sliders) InstallIn(GameObject root)
    {
        int buttons = 0, sliders = 0;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ButtonClickSound>() == null)
            {
                button.gameObject.AddComponent<ButtonClickSound>();
                buttons++;
            }
        }

        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
        {
            if (slider.GetComponent<SliderTickSound>() == null)
            {
                slider.gameObject.AddComponent<SliderTickSound>();
                sliders++;
            }
        }

        return (buttons, sliders);
    }
}
