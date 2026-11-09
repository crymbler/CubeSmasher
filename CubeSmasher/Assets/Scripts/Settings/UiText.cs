using UnityEngine.Localization;
using YG;

/// <summary>
/// Текст интерфейса из String Table "UI". Язык выбирает Unity Localization,
/// YG2LocaleBridge переключает его по YG2.lang.
///
/// Важно: если ключа нет в таблице, Unity Localization возвращает НЕ пустую строку,
/// а текст ошибки вида "No translation found for 'stat.speed' in UI". Раньше он
/// попадал прямо в интерфейс. Теперь такой ответ распознаётся и заменяется
/// запасным текстом из таблицы ниже.
/// </summary>
public static class UiText
{
    public const string Table = "UI";

    private const string MissingPrefix = "No translation found";

    /// <summary>
    /// Запасные тексты: ключ -> (ru, en, tr). Работают, пока ключа нет в String Table.
    /// Добавите ключ в таблицу - фолбэк перестанет использоваться.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, (string ru, string en, string tr)> Fallback =
        new System.Collections.Generic.Dictionary<string, (string ru, string en, string tr)>
        {
            { "gar.select",        ("Выбрать", "Select", "Seç") },
            { "gar.selected",      ("Выбрано", "Selected", "Seçildi") },

            { "stat.damage",       ("Урон", "Damage", "Hasar") },
            { "stat.radius",       ("Радиус", "Radius", "Yarıçap") },
            { "stat.crit",         ("Шанс крита", "Crit chance", "Kritik şansı") },
            { "stat.crit_damage",  ("Сила крита", "Crit damage", "Kritik hasarı") },
            { "stat.speed",        ("Скорость", "Speed", "Hız") },
            { "stat.parts",        ("Детали", "Parts", "Parçalar") },
            { "stat.delay",        ("Задержка", "Delay", "Gecikme") },

            { "unit.sec",          ("с", "s", "sn") },
            { "unit.m",            ("м", "m", "m") },
            { "unit.percent",      ("%", "%", "%") },
        };

    public static string Get(string key, params object[] args)
    {
        var ls = new LocalizedString(Table, key);
        if (args != null && args.Length > 0) ls.Arguments = args;

        string text = ls.GetLocalizedString();

        // Пусто, вернулся сам ключ или Unity сказал "перевода нет" - берём фолбэк
        if (string.IsNullOrEmpty(text) || text == key || IsMissing(text))
            return FallbackText(key, args);

        return text;
    }

    /// <summary>Unity Localization сообщает об отсутствии перевода текстом, а не null.</summary>
    private static bool IsMissing(string text)
        => text.StartsWith(MissingPrefix, System.StringComparison.OrdinalIgnoreCase);

    private static string FallbackText(string key, object[] args)
    {
        if (!Fallback.TryGetValue(key, out var v)) return Humanize(key);

        string text = YG2.lang switch
        {
            "en" => v.en,
            "tr" => v.tr,
            _    => v.ru,
        };

        if (args != null && args.Length > 0)
        {
            try { text = string.Format(text, args); }
            catch (System.FormatException) { }
        }
        return text;
    }

    /// <summary>
    /// Ключа нет совсем (например, новый стат без перевода): показываем последний сегмент
    /// ключа с заглавной буквы - "stat.attack_speed" -> "Attack speed". Лучше, чем сырой ключ.
    /// </summary>
    private static string Humanize(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;

        int dot = key.LastIndexOf('.');
        string tail = dot >= 0 && dot < key.Length - 1 ? key.Substring(dot + 1) : key;
        tail = tail.Replace('_', ' ');

        return tail.Length > 0
            ? char.ToUpperInvariant(tail[0]) + tail.Substring(1)
            : key;
    }
}
