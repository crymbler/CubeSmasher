using System.Collections.Generic;
using YG;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Таблица текстов игры на ru / en / tr. Язык берётся из YG2.lang (его выбирает плагин).
    /// Ключи — латиницей, через точку. Если ключа или языка нет, возвращается английский,
    /// а если и его нет — сам ключ (чтобы пропуск был виден, а не пустой).
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Table =
            new Dictionary<string, Dictionary<string, string>>
        {
            { "gar.status.open",    new Dictionary<string,string> { {"ru","Открыто"},        {"en","Open"},             {"tr","Açık"} } },
            { "gar.status.locked",  new Dictionary<string,string> { {"ru","Откроется на стадии {0}"}, {"en","Unlocks at stage {0}"}, {"tr","{0}. aşamada açılır"} } },
            { "gar.income",         new Dictionary<string,string> { {"ru","Доход: {0}/сек"}, {"en","Income: {0}/s"},   {"tr","Gelir: {0}/sn"} } },
            { "gar.income.none",    new Dictionary<string,string> { {"ru","Доход: —"},       {"en","Income: —"},        {"tr","Gelir: —"} } },
            { "gar.open",           new Dictionary<string,string> { {"ru","ОТКРЫТЬ"},        {"en","OPEN"},             {"tr","AÇ"} } },
            { "gar.open.hint",      new Dictionary<string,string> { {"ru","Машина ждёт открытия"}, {"en","Ready to open"}, {"tr","Açılmaya hazır"} } },
            { "gar.status.available",new Dictionary<string,string> { {"ru","Доступна"},      {"en","Available"},        {"tr","Mevcut"} } },
            { "gar.locked.hint",    new Dictionary<string,string> { {"ru","Ещё {0} стадий"},  {"en","{0} stages to go"},  {"tr","{0} aşama kaldı"} } },
            { "shop.level",         new Dictionary<string,string> { {"ru","Ур. {0}"},        {"en","Lv. {0}"},          {"tr","Sv. {0}"} } },
            { "shop.max",           new Dictionary<string,string> { {"ru","МАКС"},           {"en","MAX"},              {"tr","MAKS"} } },
            { "shop.unlock",        new Dictionary<string,string> { {"ru","Стадия {0}"},     {"en","Stage {0}"},        {"tr","Aşama {0}"} } },
            { "stage.machine",      new Dictionary<string,string> { {"ru","Машина {0}"},     {"en","Machine {0}"},      {"tr","Makine {0}"} } },
            { "reward.compensation",new Dictionary<string,string> { {"ru","Вам начислена компенсация"}, {"en","You received a compensation"}, {"tr","Tazminat hesabınıza eklendi"} } },
            { "hud.income",         new Dictionary<string,string> { {"ru","+{0}/сек"},       {"en","+{0}/s"},           {"tr","+{0}/sn"} } },
        };

        /// <summary>Текущий язык из плагина: ru / en / tr. По умолчанию ru.</summary>
        public static string CurrentLang
        {
            get
            {
                string l = YG2.lang;
                return string.IsNullOrEmpty(l) ? "ru" : l;
            }
        }

        /// <summary>Вернуть строку по ключу на текущем языке, с подстановкой {0}, {1}...</summary>
        public static string Get(string key, params object[] args)
        {
            if (!Table.TryGetValue(key, out var byLang)) return key;

            string lang = CurrentLang;
            if (!byLang.TryGetValue(lang, out string text))
            {
                if (!byLang.TryGetValue("en", out text)) return key;
            }

            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }
    }
}
