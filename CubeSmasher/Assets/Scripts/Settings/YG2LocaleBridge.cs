using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using YG;

/// <summary>
/// Связывает язык YG2 с Unity Localization.
/// Ставить на сцену не нужно: объект создаётся сам при запуске игры и живёт между сценами.
/// </summary>
public class YG2LocaleBridge : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<YG2LocaleBridge>() != null) return;

        var go = new GameObject("YG2LocaleBridge");
        DontDestroyOnLoad(go);
        go.AddComponent<YG2LocaleBridge>();
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        if (!string.IsNullOrEmpty(YG2.lang)) Apply(YG2.lang);
    }

    private void OnEnable() => YG2.onSwitchLang += Apply;
    private void OnDisable() => YG2.onSwitchLang -= Apply;

    public static void Apply(string lang)
    {
        if (!LocalizationSettings.InitializationOperation.IsDone) return;

        var locales = LocalizationSettings.AvailableLocales;
        var locale = locales.GetLocale(lang) ?? locales.GetLocale("en");
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
    }
}
