using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Статичная подпись с переводом из таблицы UI.
    ///
    /// Зачем отдельный компонент: в редакторе таблица локализации не загружена,
    /// поэтому `UiText.Get("settings.title")` возвращает сам ключ. Сборщик
    /// записывал этот ключ в префаб как текст, и в игре вместо «НАСТРОЙКИ»
    /// было «settings.title». Здесь текст ставится в рантайме, когда таблица
    /// уже доступна.
    ///
    /// Смена языка: подписка на `LocalizationSettings.SelectedLocaleChanged`.
    /// Таблица нового языка грузится асинхронно, поэтому перечитывать текст
    /// в момент нажатия кнопки бессмысленно — там ещё старая таблица.
    /// Событие приходит, когда локаль уже переключена, и текст обновляется сам.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UiTextLabel : MonoBehaviour
    {
        [Tooltip("Ключ из таблицы UI, например settings.title")]
        public string Key;

        private TextMeshProUGUI _label;
        private Coroutine _refresh;

        /// <summary>Повтор уже был: второй раз не просим, иначе бесконечный цикл.</summary>
        private bool _retried;

        private void Awake() => Apply();

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            Apply();
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

            _retried = false;
            RequestRefresh();
        }

        /// <summary>
        /// Перечитать текст. Вызывается при включении, при смене языка
        /// и вручную через <see cref="SetKey"/>.
        /// </summary>
        public void Apply()
        {
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            if (_label == null || string.IsNullOrEmpty(Key)) return;

            string text = UiText.Get(Key);

            // Перевод не найден — UiText вернул сам ключ. Таблица могла ещё
            // не загрузиться: пробуем ещё раз на следующем кадре, но только раз.
            if (text == Key)
            {
                if (!_retried) RequestRefresh();
                return;
            }

            _retried = false;
            _label.text = text;
        }

        /// <summary>Сменить ключ и сразу перечитать текст.</summary>
        public void SetKey(string key)
        {
            Key = key;
            _retried = false;
            Apply();
        }

        private void RequestRefresh()
        {
            if (!isActiveAndEnabled) return;

            if (_refresh != null) StopCoroutine(_refresh);
            _refresh = StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;

            _refresh = null;
            _retried = true;
            Apply();
        }
    }
}
