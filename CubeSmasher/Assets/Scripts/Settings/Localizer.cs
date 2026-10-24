using TMPro;
using UnityEngine;
using YG;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Компонент для TMP-текста, который показывает строку по ключу из Localization.
    /// Обновляется при старте и при смене языка через YG2.onSwitchLang.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class Localizer : MonoBehaviour
    {
        [Tooltip("Ключ из Localization.Table, например gar.status.open")]
        [SerializeField] private string _key;

        private TextMeshProUGUI _text;
        private object[] _args;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            YG2.onSwitchLang -= OnLanguageChanged;
        }

        private void OnLanguageChanged(string _) => Refresh();

        /// <summary>Задать аргументы {0}, {1}... и перерисовать текст. Пример: SetArgs(12, "с").</summary>
        public void SetArgs(params object[] args)
        {
            _args = args;
            Refresh();
        }

        /// <summary>Перерисовать текст по текущему ключу и языку.</summary>
        public void Refresh()
        {
            if (_text == null || string.IsNullOrEmpty(_key)) return;
            _text.text = Localization.Get(_key, _args);
        }
    }
}
