using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using YG;

/// <summary>
/// Связывает язык YG2 с Unity Localization.
/// Ставить на сцену не нужно: объект создаётся сам при запуске игры
/// и живёт между сценами.
///
/// Здесь есть неочевидное место. В `Lang_yg.InitLang` под `#if UNITY_EDITOR`
/// стоит `onSwitchLang = null` — плагин чистит делегат, чтобы в редакторе
/// с выключенной перезагрузкой домена не оставалось старых подписок. Если
/// инициализация YG2 проходит ПОСЛЕ нашей подписки из OnEnable, подписка
/// теряется, и смена языка молча перестаёт работать: в сборке работает,
/// в редакторе нет. Поэтому в Start подписываемся ещё раз.
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

        // Повторная подписка: YG2 мог обнулить делегат уже после OnEnable
        Subscribe();

        if (!string.IsNullOrEmpty(YG2.lang)) Apply(YG2.lang);
    }

    private void OnEnable() => Subscribe();

    private void OnDisable() => YG2.onSwitchLang -= Apply;

    /// <summary>
    /// Подписаться ровно один раз: `+=` без снятия накапливает вызовы,
    /// и после нескольких входов Apply срабатывает несколько раз подряд.
    /// </summary>
    private static void Subscribe()
    {
        YG2.onSwitchLang -= Apply;
        YG2.onSwitchLang += Apply;
    }

    /// <summary>
    /// Переключить Unity Localization на язык YG2. Вызывается автоматически
    /// при смене языка и вручную из экрана настроек — на случай, если подписка
    /// на событие потерялась.
    /// </summary>
    public static void Apply(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return;

        // Раньше здесь стоял молчаливый `return`: если локализация ещё не
        // инициализировалась, смена языка просто не происходила, и в Console
        // не было ни строчки. Теперь каждый исход виден.
        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            Debug.LogWarning("[Locale] Локализация ещё не готова, смена отложена: " + lang);
            return;
        }

        var locales = LocalizationSettings.AvailableLocales;

        if (locales == null)
        {
            Debug.LogWarning("[Locale] Список локалей пуст. Проверьте " +
                             "Window → Asset Management → Localization Scene Controls.");
            return;
        }

        var locale = locales.GetLocale(lang) ?? locales.GetLocale("en");

        if (locale == null)
        {
            Debug.LogWarning("[Locale] Нет локали для языка '" + lang +
                             "'. Проверьте Assets/Localisation: должны быть ru, en, tr.");
            return;
        }

        if (LocalizationSettings.SelectedLocale == locale)
        {
            Debug.Log("[Locale] Язык уже активен: " + locale.Identifier.Code);
            return;
        }

        LocalizationSettings.SelectedLocale = locale;
        Debug.Log("[Locale] Язык переключён на " + locale.Identifier.Code);
    }
}
