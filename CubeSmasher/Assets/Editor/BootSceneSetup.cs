using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CubeSmasher.Core;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Setup Boot Scene
/// Настраивает сцену загрузки под текущий BootScreen:
///  - градиентный фон вместо плоской заливки;
///  - цвет камеры в тон градиента;
///  - область водопада в долях экрана, скорость, размер кубов, глубина;
///  - минимальное время показа 1.5 с.
/// Открывает Assets/Scenes/Boot.unity, если она не открыта. Повторный запуск безопасен.
/// </summary>
public static class BootSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boot.unity";
    private const float MinShowSeconds = 1.5f;

    // Глубина водопада должна быть заметно меньше planeDistance у Canvas (там 100),
    // иначе фон окажется перед кубами.
    private const float Depth = 25f;
    private const float AreaWidth = 1f;
    private const float BottomReserve = 0.2f;
    private const float FallSpeed = 0.55f;
    private const float SizeMin = 0.10f;
    private const float SizeMax = 0.20f;

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
            if (background.GetComponent<Image>() == null)
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
            var camSer = new SerializedObject(camera);
            SerializedProperty clear = camSer.FindProperty("m_ClearFlags");
            SerializedProperty bg = camSer.FindProperty("m_BackGroundColor");
            if (clear != null) clear.intValue = 1;
            if (bg != null) bg.colorValue = UITheme.SkyTop;
            camSer.ApplyModifiedPropertiesWithoutUndo();
            changes++;
        }

        BootScreen boot = Object.FindFirstObjectByType<BootScreen>();
        if (boot != null)
        {
            var ser = new SerializedObject(boot);
            changes += SetFloat(ser, "_minShowSeconds", MinShowSeconds);
            changes += SetFloat(ser, "_depth", Depth);
            changes += SetFloat(ser, "_areaWidth", AreaWidth);
            changes += SetFloat(ser, "_bottomReserve", BottomReserve);
            changes += SetFloat(ser, "_fallSpeed", FallSpeed);

            SerializedProperty size = ser.FindProperty("_sizeFraction");
            if (size != null)
            {
                size.vector2Value = new Vector2(SizeMin, SizeMax);
                changes++;
            }

            SerializedProperty depthSpread = ser.FindProperty("_depthSpread");
            if (depthSpread != null)
            {
                depthSpread.vector2Value = new Vector2(-4f, 6f);
                changes++;
            }

            SerializedProperty cam = ser.FindProperty("_camera");
            if (cam != null && cam.objectReferenceValue == null && camera != null)
            {
                cam.objectReferenceValue = camera;
                changes++;
            }

            ser.ApplyModifiedPropertiesWithoutUndo();
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

    private static int SetFloat(SerializedObject ser, string name, float value)
    {
        SerializedProperty prop = ser.FindProperty(name);
        if (prop == null)
        {
            Debug.LogWarning("[Boot] Поле " + name + " не найдено в BootScreen.");
            return 0;
        }
        if (Mathf.Approximately(prop.floatValue, value)) return 0;

        prop.floatValue = value;
        return 1;
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
