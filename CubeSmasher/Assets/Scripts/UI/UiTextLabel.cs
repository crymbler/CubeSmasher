using System.Collections;
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
    /// ПОЧЕМУ РАНЬШЕ ЯЗЫК «НЕ МЕНЯЛСЯ».
    /// `UiText.Get` синхронный, а таблица языка грузится асинхронно через
    /// Addressables. В момент смены языка она ещё не загружена, и
    /// `GetLocalizedString` возвращает либо сам ключ, либо текст ПРЕЖНЕГО языка.
    /// Прежняя версия в этом случае молча выходила по `return`, не трогая
    /// `_label.text` — на экране оставался старый текст.
    ///
    /// Здесь мы не гадаем по кадрам: запрашиваем таблицу ИМЕННО выбранного
    /// языка (`GetTableAsync(Table, locale)`), дожидаемся её и читаем строку
    /// прямо из неё. Загруженная таблица кэшируется, повтор дешёвый.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UiTextLabel : MonoBehaviour
    {
        [Tooltip("Ключ из таблицы UI, например settings.title")]
        public string Key;

        private TextMeshProUGUI _label;
        private Coroutine _refresh;

        /// <summary>Для какого языка текст уже применён.</summary>
        private string _appliedLocale;

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

            if (_refresh != null)
            {
                StopCoroutine(_refresh);
                _refresh = null;
            }
        }

        private void HandleLocaleChanged(Locale locale)
        {
            if (!isActiveAndEnabled) return;

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
            if (!isActiveAndEnabled) return;
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            if (_label == null || string.IsNullOrEmpty(Key)) return;

            if (_refresh != null) StopCoroutine(_refresh);
            _refresh = StartCoroutine(ApplyWhenTableReady());
        }

        private IEnumerator ApplyWhenTableReady()
        {
            Locale locale = LocalizationSettings.SelectedLocale;

            if (locale == null)
            {
                Debug.LogWarning("[Locale] Язык не выбран, текст '" + Key + "' не обновлён.");
                _refresh = null;
                yield break;
            }

            string code = locale.Identifier.Code;

            if (_appliedLocale == code) { _refresh = null; yield break; }

            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                yield return LocalizationSettings.InitializationOperation;
            }

            AsyncOperationHandle<StringTable> handle =
                LocalizationSettings.StringDatabase.GetTableAsync(UiText.Table, locale);

            yield return handle;

            _refresh = null;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogWarning("[Locale] Таблица '" + UiText.Table + "' для языка '" + code +
                                 "' не загрузилась. Ключ: " + Key);
                yield break;
            }

            StringTableEntry entry = handle.Result.GetEntry(Key);

            if (entry == null)
            {
                Debug.LogWarning("[Locale] В таблице '" + UiText.Table + "' нет ключа '" + Key +
                                 "' для языка '" + code + "'.");
                yield break;
            }

            _label.text = entry.GetLocalizedString();
            _appliedLocale = code;
        }
    }
}
