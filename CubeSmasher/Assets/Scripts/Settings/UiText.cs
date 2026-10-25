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

    public static string Get(string key, params object[] args)
    {
        var ls = new LocalizedString(Table, key);
        if (args != null && args.Length > 0) ls.Arguments = args;

        string text = ls.GetLocalizedString();
        return string.IsNullOrEmpty(text) ? key : text;
    }
}
