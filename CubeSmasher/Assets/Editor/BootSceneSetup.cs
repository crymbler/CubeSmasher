using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CubeSmasher.Core;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Setup Boot Scene
/// Приводит сцену загрузки в порядок для K10:
///  - ставит GradientBackground на объект Background;
///  - красит камеру в цвет неба, чтобы фон совпадал с градиентом;
///  - ставит минимальное время показа экрана 1.5 с вместо 10.
/// Открывает Assets/Scenes/Boot.unity, если она не открыта. Повторный запуск не дублирует.
/// </summary>
public static class BootSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boot.unity";
    private const float MinShowSeconds = 1.5f;

    [MenuItem("Tools/CubeSmasher/Setup Boot Scene")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        int changes = 0;

        GameObject background = FindByName(scene, "Background");
        if (background != null)
        {
            var image = background.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning("[Boot] У Background нет Image: градиент не поставить.");
            }
            else if (background.GetComponent<GradientBackground>() == null)
            {
                background.AddComponent<GradientBackground>();
                changes++;
                Debug.Log("[Boot] GradientBackground добавлен на Background.");
            }
        }
        else
        {
            Debug.LogWarning("[Boot] Объект Background не найден.");
        }

        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            var ser = new SerializedObject(camera);
            SerializedProperty clear = ser.FindProperty("m_ClearFlags");
            SerializedProperty color = ser.FindProperty("m_BackGroundColor");
            if (clear != null) clear.intValue = 1;
            if (color != null) color.colorValue = UITheme.SkyTop;
            ser.ApplyModifiedPropertiesWithoutUndo();
            changes++;
        }

        BootScreen boot = Object.FindFirstObjectByType<BootScreen>();
        if (boot != null)
        {
            var ser = new SerializedObject(boot);
            SerializedProperty minShow = ser.FindProperty("_minShowSeconds");
            if (minShow != null && !Mathf.Approximately(minShow.floatValue, MinShowSeconds))
            {
                float was = minShow.floatValue;
                minShow.floatValue = MinShowSeconds;
                ser.ApplyModifiedPropertiesWithoutUndo();
                changes++;
                Debug.Log("[Boot] _minShowSeconds: было " + was + ", стало " + MinShowSeconds);
            }
        }
        else
        {
            Debug.LogWarning("[Boot] На сцене нет BootScreen.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Boot] Готово. Изменений: " + changes + ". Сцена сохранена.");
    }

    private static GameObject FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t.gameObject;
            }
        }
        return null;
    }
}
