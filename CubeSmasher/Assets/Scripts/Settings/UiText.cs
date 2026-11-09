using UnityEngine.Localization;
using YG;

/// <summary>
/// Текст интерфейса из String Table "UI". Язык выбирает Unity Localization,
/// YG2LocaleBridge переключает его по YG2.lang.
/// Ключа нет или таблица не загружена - берётся запасной текст из таблицы ниже,
/// чтобы в интерфейсе не торчали ключи вида "stat.damage".
/// Аргументы: UiText.Get("shop.level", 3) даёт "Lv. 3" / "Ур. 3" по ключу "{0}".
/// </summary>
public static class UiText
{
    public const string Table = "UI";

    /// <summary>
    /// Запасные тексты: ключ -> (ru, en, tr). Нужны, пока ключ не добавлен в String Table.
    /// Добавите в таблицу - фолбэк перестанет использоваться.
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
        if (string.IsNullOrEmpty(text) || text == key) return FallbackText(key, args);

        return text;
    }

    private static string FallbackText(string key, object[] args)
    {
        if (!Fallback.TryGetValue(key, out var v)) return key;

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
}
