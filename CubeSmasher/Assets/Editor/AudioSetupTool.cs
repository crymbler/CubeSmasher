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
/// 1. Назначает звуки из Assets/Audio на Bootstrapper по именам файлов:
///    BackgroundMusic -> _musicClip, Click -> _clickClip, HammerHit -> _hammerHitClip,
///    Unlock -> _unlockClip. Split пока отсутствует: поле останется пустым, пока файл не добавлен
///    (в Assets/Audio/Sounds/Split.mp3 или Split.wav).
///    Поля, которые уже назначены вручную, не перезаписываются.
/// 2. Ставит ButtonClickSound на все Button и SliderTickSound на все Slider
///    в открытой сцене и во всех префабах. Повторный запуск ничего не дублирует.
///    Кнопкам клип не назначается: они берут общий Click из AudioClipLibrary.
/// </summary>
public static class AudioSetupTool
{
    private const string AudioRoot = "Assets/Audio";

    [MenuItem("Tools/CubeSmasher/Setup Audio")]
    public static void Setup()
    {
        int assigned = AssignBootstrapperClips();

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
        Debug.Log("[Audio] Готово. Клипов назначено: " + assigned +
                  ", кнопок: " + buttons + ", слайдеров: " + sliders);
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
