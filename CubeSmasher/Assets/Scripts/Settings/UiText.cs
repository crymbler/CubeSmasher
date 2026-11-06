using UnityEngine.Localization;

/// <summary>
/// Текст интерфейса из String Table "UI". Язык выбирает Unity Localization,
/// YG2LocaleBridge переключает его по YG2.lang.
/// Ключа нет или таблица ещё не загружена — возвращается сам ключ.
/// Аргументы: UiText.Get("shop.level", 3) даёт "Lv. 3" / "Ур. 3" по ключу "{0}".
/// </summary>
public static class UiText
{
    public const string Table = "UI";

    /// <summary>Текст на случай, если ключа ещё нет в таблице локализации.</summary>
    private static readonly System.Collections.Generic.Dictionary<string, string> Fallback =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "gar.select", "Выбрать" },
            { "gar.selected", "Выбрано" },
        };

    public static string Get(string key, params object[] args)
    {
        var ls = new LocalizedString(Table, key);
        if (args != null && args.Length > 0) ls.Arguments = args;

        string text = ls.GetLocalizedString();

        // Ключа может не быть в таблице: показываем запасной текст,
        // чтобы в интерфейсе не торчало "gar.select"
        if (string.IsNullOrEmpty(text) || text == key)
            return Fallback.TryGetValue(key, out string fallback) ? fallback : key;

        return text;
    }
}
