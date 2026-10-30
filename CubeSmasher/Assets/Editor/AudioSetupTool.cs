using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CubeSmasher.Core;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Setup Audio
///
/// Порядок важен: сначала префабы, потом сцена. Если сделать наоборот, компонент
/// добавится в сцену как override поверх инстанса префаба, а потом ещё раз
/// в сам префаб — и в Play Mode на одном объекте окажется два компонента.
///
/// Объекты, которые уже являются частью инстанса префаба, пропускаются:
/// они получат компонент из самого префаба.
///
/// Отдельный проход снимает лишние копии, если они уже успели появиться.
/// </summary>
public static class AudioSetupTool
{
    private const string AudioRoot = "Assets/Audio";

    [MenuItem("Tools/CubeSmasher/Setup Audio")]
    public static void Setup()
    {
        int assigned = AssignBootstrapperClips();
        int buttons = 0, sliders = 0, removed = 0;

        // 1. Префабы первыми: они источник компонентов для своих инстансов
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            (int b, int s) = InstallIn(root);
            removed += RemoveDuplicatesIn(root);

            if (b > 0 || s > 0 || removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                buttons += b;
                sliders += s;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // 2. Сцена: только собственные объекты, не части инстансов
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                (int b, int s) = InstallIn(root);
                removed += RemoveDuplicatesIn(root);
                buttons += b;
                sliders += s;
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Audio] Готово. Клипов назначено: " + assigned +
                  ", кнопок: " + buttons + ", слайдеров: " + sliders +
                  ", лишних компонентов снято: " + removed);
    }

    /// <summary>Находит Bootstrapper в открытой сцене и заполняет пустые поля клипами по имени файла.</summary>
    private static int AssignBootstrapperClips()
    {
        Bootstrapper bootstrapper = Object.FindFirstObjectByType<Bootstrapper>();
        if (bootstrapper == null)
        {
            Debug.LogWarning("[Audio] На сцене нет Bootstrapper: клипы не назначены.");
            return 0;
        }

        var so = new SerializedObject(bootstrapper);
        int count = 0;

        count += AssignIfEmpty(so, "_musicClip", "BackgroundMusic");
        count += AssignIfEmpty(so, "_clickClip", "Click");
        count += AssignIfEmpty(so, "_hammerHitClip", "HammerHit");
        count += AssignIfEmpty(so, "_unlockClip", "Unlock");
        count += AssignIfEmpty(so, "_splitClip", "Split");

        if (count > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bootstrapper);
        }
        return count;
    }

    private static int AssignIfEmpty(SerializedObject so, string field, string fileName)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue != null) return 0;

        AudioClip clip = FindClip(fileName);
        if (clip == null)
        {
            Debug.Log("[Audio] Файл " + fileName + " не найден в " + AudioRoot + ", поле " + field + " пустое.");
            return 0;
        }

        prop.objectReferenceValue = clip;
        return 1;
    }

    private static AudioClip FindClip(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:AudioClip", new[] { AudioRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == fileName)
            {
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
        return null;
    }

    /// <summary>
    /// Ставит компоненты только на собственные объекты. Всё, что является частью
    /// инстанса префаба (в префабе — вложенный префаб, в сцене — инстанс префаба),
    /// пропускается: компонент придёт из самого префаба-источника.
    /// </summary>
    private static (int buttons, int sliders) InstallIn(GameObject root)
    {
        int buttons = 0, sliders = 0;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(button.gameObject)) continue;
            if (button.GetComponent<ButtonClickSound>() != null) continue;

            button.gameObject.AddComponent<ButtonClickSound>();
            buttons++;
        }

        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(slider.gameObject)) continue;
            if (slider.GetComponent<SliderTickSound>() != null) continue;

            slider.gameObject.AddComponent<SliderTickSound>();
            sliders++;
        }

        return (buttons, sliders);
    }

    /// <summary>Снимает лишние копии, оставляя по одному компоненту каждого типа.</summary>
    private static int RemoveDuplicatesIn(GameObject root)
    {
        int removed = 0;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;

            removed += RemoveExtras<ButtonClickSound>(t.gameObject);
            removed += RemoveExtras<SliderTickSound>(t.gameObject);
        }

        return removed;
    }

    private static int RemoveExtras<T>(GameObject go) where T : Component
    {
        T[] found = go.GetComponents<T>();
        if (found.Length <= 1) return 0;

        for (int i = 1; i < found.Length; i++)
        {
            Object.DestroyImmediate(found[i]);
        }
        return found.Length - 1;
    }
}
