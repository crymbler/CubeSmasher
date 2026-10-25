using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using YG;

/// <summary>
/// Связывает язык YG2 с Unity Localization. Ставится один раз на объект первой сцены.
/// Пока язык не выбран в YG2, использует текущий язык Unity Localization.
/// </summary>
public class YG2LocaleBridge : MonoBehaviour
{
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
