using UnityEngine;
using YG;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Резервная копия прогресса в PlayerPrefs. Используется, когда облачное
    /// сохранение Яндекса не удалось. Пишется сразу, поэтому прогресс не теряется.
    /// </summary>
    public static class LocalSaveService
    {
        public const string Key = "smasher_local";

        /// <summary>Записать текущий YG2.saves в PlayerPrefs.</summary>
        public static void Write()
        {
            string json = JsonUtility.ToJson(YG2.saves);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        /// <summary>Есть ли локальная копия.</summary>
        public static bool Exists() => PlayerPrefs.HasKey(Key);

        /// <summary>Прочитать копию и вернуть её как JSON, либо null.</summary>
        public static string Read() => Exists() ? PlayerPrefs.GetString(Key) : null;

        /// <summary>Удалить копию после успешной отправки в облако.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
