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

        // Таблица грузится асинхронно: до готовности ничего не трогаем
        if (!LocalizationSettings.InitializationOperation.IsDone) return;

        var locales = LocalizationSettings.AvailableLocales;
        if (locales == null) return;

        var locale = locales.GetLocale(lang) ?? locales.GetLocale("en");

        if (locale == null)
        {
            Debug.LogWarning("[Locale] В проекте нет локали для языка '" + lang +
                             "'. Проверьте Assets/Localisation: должны быть ru, en, tr.");
            return;
        }

        // Повторная установка той же локали не шлёт SelectedLocaleChanged,
        // поэтому проверяем явно
        if (LocalizationSettings.SelectedLocale != locale)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
    }
}
