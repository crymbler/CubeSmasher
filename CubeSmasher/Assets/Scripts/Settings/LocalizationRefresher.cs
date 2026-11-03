using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Точка «язык сменился, таблица нового языка уже загружена».
    ///
    /// ЗАЧЕМ. Экраны пишут переведённый текст через UiText.Get в тот момент,
    /// когда меняются ДАННЫЕ: пришёл доход, сменилась машина, обновился баланс.
    /// Смена языка данными не является — эти методы повторно не вызываются,
    /// и на экране остаётся текст прежнего языка. Отсюда впечатление, что нужна
    /// перерисовка. И это правда перерисовка — но не перезагрузка сцены,
    /// а повторная передача тех же данных во View, чтобы он перечитал переводы.
    ///
    /// ПОЧЕМУ НЕ СРАЗУ ПО СОБЫТИЮ СМЕНЫ ЛОКАЛИ. Таблица нового языка грузится
    /// асинхронно. Если дёрнуть UiText.Get в этот момент, вернётся либо ключ,
    /// либо текст ПРЕЖНЕГО языка. Поэтому сначала ждём загрузку таблицы.
    /// </summary>
    public static class LocalizationRefresher
    {
        /// <summary>Таблица нового языка загружена — можно перерисовывать экраны.</summary>
        public static event Action LanguageReady;

        private static bool _installed;

        /// <summary>
        /// Номер текущего запроса. Если язык успели сменить ещё раз, пока
        /// грузилась таблица, ответ на старый запрос отбрасывается.
        /// </summary>
        private static int _requestId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_installed) return;
            _installed = true;

            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        }

        /// <summary>
        /// Снять всех подписчиков. Вызывается при сборке сцены: иначе после
        /// перезагрузки остались бы ссылки на уничтоженные презентеры.
        /// </summary>
        public static void Reset()
        {
            LanguageReady = null;
            _requestId++;
        }

        /// <summary>Сообщить экранам, что язык сменился.</summary>
        public static void Notify()
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            if (locale != null) Load(locale, ++_requestId);
        }

        private static void HandleLocaleChanged(Locale locale)
        {
            if (locale == null) return;

            int requestId = ++_requestId;

            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                AsyncOperationHandle init = LocalizationSettings.InitializationOperation;
                init.Completed += _ => Load(locale, requestId);
                return;
            }

            Load(locale, requestId);
        }

        private static void Load(Locale locale, int requestId)
        {
            AsyncOperationHandle<StringTable> handle =
                LocalizationSettings.StringDatabase.GetTableAsync(UiText.Table, locale);

            handle.Completed += h =>
            {
                // Пока грузилось, язык сменили ещё раз — этот ответ устарел
                if (requestId != _requestId) return;

                if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null)
                {
                    Debug.LogWarning("[Locale] Таблица '" + UiText.Table + "' для языка '" +
                                     locale.Identifier.Code + "' не загрузилась: " +
                                     "экраны останутся на прежнем языке.");
                    return;
                }

                LanguageReady?.Invoke();
            };
        }
    }
}
