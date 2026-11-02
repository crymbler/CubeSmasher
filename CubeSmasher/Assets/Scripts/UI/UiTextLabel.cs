using TMPro;
using UnityEngine;

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
    /// Работает и для заголовков, и для подписей ползунков.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UiTextLabel : MonoBehaviour
    {
        [Tooltip("Ключ из таблицы UI, например settings.title")]
        public string Key;

        private TextMeshProUGUI _label;

        private void Awake() => Apply();

        /// <summary>
        /// Перечитать текст. Нужно при смене языка без перезагрузки сцены
        /// и при установке ключа из кода.
        /// </summary>
        public void Apply()
        {
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            if (_label == null || string.IsNullOrEmpty(Key)) return;

            _label.text = UiText.Get(Key);
        }

        /// <summary>Сменить ключ и сразу перечитать текст.</summary>
        public void SetKey(string key)
        {
            Key = key;
            Apply();
        }
    }
}
