using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Статичная подпись с переводом из таблицы UI.
    ///
    /// Зачем отдельный компонент: в редакторе таблица локализации не загружена,
    /// поэтому `UiText.Get("settings.title")` возвращает сам ключ. Сборщик
    /// записывал этот ключ в префаб как текст, и в игре вместо «НАСТРОЙКИ»
    /// было «settings.title».
    ///
    /// ПОЧЕМУ ЯЗЫК НЕ МЕНЯЛСЯ. Две причины:
    ///
    /// 1. `UiText.Get` синхронный, а таблица грузится асинхронно через
    ///    Addressables. Сразу после смены языка таблица нового языка ещё не
    ///    загружена, и `GetLocalizedString` возвращает текст ПРЕЖНЕГО языка.
    ///    Поэтому здесь берётся таблица ИМЕННО выбранного языка через
    ///    `GetTableAsync(Table, locale)`, и строка читается прямо из неё.
    ///
    /// 2. Обновление шло через корутину. Корутины не идут на выключенном
    ///    объекте, а окна закрываются через `SetActive(false)`. Здесь вместо
    ///    корутины подписка на `AsyncOperationHandle.Completed`: ответ придёт,
    ///    когда таблица загрузится, независимо от активности объекта и от
    ///    `Time.timeScale`.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UiTextLabel : MonoBehaviour
    {
        [Tooltip("Ключ из таблицы UI, например settings.title")]
        public string Key;

        private TextMeshProUGUI _label;

        /// <summary>Для какого языка текст уже стоит в поле.</summary>
        private string _appliedLocale;

        /// <summary>
        /// Номер текущего запроса. Растёт на каждый новый — так поздние ответы
        /// на устаревшие запросы отбрасываются и не перетирают свежий текст.
        /// </summary>
        private int _requestId;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            RequestRefresh();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

            // Объект выключили: ответы на незавершённые запросы больше не нужны
            _requestId++;
        }

        private void HandleLocaleChanged(Locale locale)
        {
            _appliedLocale = null;
            RequestRefresh();
        }

        /// <summary>Перечитать текст. Безопасно вызывать часто.</summary>
        public void Apply()
        {
            RequestRefresh();
        }

        /// <summary>Сменить ключ и сразу перечитать текст.</summary>
        public void SetKey(string key)
        {
            Key = key;
            _appliedLocale = null;
            RequestRefresh();
        }

        private void RequestRefresh()
        {
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            if (_label == null || string.IsNullOrEmpty(Key)) return;

            Locale locale = LocalizationSettings.SelectedLocale;

            if (locale == null)
            {
                Warn("язык не выбран");
                return;
            }

            string code = locale.Identifier.Code;

            // Уже стоит текст этого языка — второй раз не грузим
            if (_appliedLocale == code) return;

            int requestId = ++_requestId;

            // Локализация могла ещё не инициализироваться: дождёмся и повторим
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                AsyncOperationHandle init = LocalizationSettings.InitializationOperation;
                init.Completed += _ =>
                {
                    if (requestId != _requestId) return;

                    Locale current = LocalizationSettings.SelectedLocale;
                    if (current == null) return;

                    Load(current, current.Identifier.Code, requestId);
                };
                return;
            }

            Load(locale, code, requestId);
        }

        /// <summary>
        /// Запросить таблицу нужного языка и применить строку по готовности.
        /// Обратный вызов вместо корутины: сработает и на выключенном объекте,
        /// и при Time.timeScale = 0.
        /// </summary>
        private void Load(Locale locale, string code, int requestId)
        {
            AsyncOperationHandle<StringTable> handle =
                LocalizationSettings.StringDatabase.GetTableAsync(UiText.Table, locale);

            handle.Completed += h =>
            {
                // Пока грузилось, язык успели сменить ещё раз — ответ устарел
                if (requestId != _requestId) return;

                Apply(h, code);
            };
        }

        private void Apply(AsyncOperationHandle<StringTable> handle, string code)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Warn("таблица '" + UiText.Table + "' для языка '" + code + "' не загрузилась");
                return;
            }

            StringTableEntry entry = handle.Result.GetEntry(Key);

            if (entry == null)
            {
                Warn("в таблице '" + UiText.Table + "' нет ключа '" + Key + "' для языка '" + code + "'");
                return;
            }

            if (_label == null) return;

            _label.text = entry.GetLocalizedString();
            _appliedLocale = code;
        }

        private void Warn(string what)
        {
            Debug.LogWarning("[Locale] " + what + " (ключ: " + Key + ", объект: " + name + ")");
        }
    }
}
