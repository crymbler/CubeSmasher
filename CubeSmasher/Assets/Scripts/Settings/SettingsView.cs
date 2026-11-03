using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;
using CubeSmasher.UI;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Экран настроек: громкость музыки, громкость звуков, выбор языка.
    ///
    /// Работает и как отдельное окно, и как панель внутри другого экрана.
    /// Значения ползунков подтягиваются из SettingsService при открытии,
    /// а каждое движение ползунка сразу применяется — отдельной кнопки
    /// «Сохранить» нет, это привычнее в мобильных играх.
    ///
    /// Язык переключается через YG2.SwitchLanguage. Он сохраняет выбор сам,
    /// а YG2LocaleBridge переключает Unity Localization. Сцена перезагружается,
    /// иначе уже созданные тексты останутся на прежнем языке.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [Header("Каркас")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject[] _hideWhileOpen;

        [Header("Громкость")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _soundSlider;
        [SerializeField] private TextMeshProUGUI _musicValue;
        [SerializeField] private TextMeshProUGUI _soundValue;

        [Header("Язык")]
        [Tooltip("Кнопки языков. Код: ru, en, tr")]
        [SerializeField] private LanguageSettingsPanel.LanguageButton[] _languageButtons;

        /// <summary>Окно закрылось.</summary>
        public event Action Closed;

        private SettingsService _settings;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);

            if (_musicSlider != null)
            {
                _musicSlider.minValue = 0f;
                _musicSlider.maxValue = 1f;
                _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }

            if (_soundSlider != null)
            {
                _soundSlider.minValue = 0f;
                _soundSlider.maxValue = 1f;
                _soundSlider.onValueChanged.AddListener(OnSoundChanged);
            }
        }

        /// <summary>Передать сервис настроек. Вызывается из UIWiringInitializer.</summary>
        public void Initialize(SettingsService settings)
        {
            _settings = settings;
            Refresh();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
            SetHidden(true);
        }

        public void Close()
        {
            // Громкость копится в памяти и пишется один раз здесь
            _settings?.Flush();

            gameObject.SetActive(false);
            SetHidden(false);
            Closed?.Invoke();
        }

        /// <summary>Подтянуть сохранённые значения в ползунки и подсветить язык.</summary>
        public void Refresh()
        {
            if (_settings != null)
            {
                // SetValueWithoutNotify: иначе установка значения вызовет
                // onValueChanged и лишний раз запишет сохранение
                if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(_settings.MusicVolume);
                if (_soundSlider != null) _soundSlider.SetValueWithoutNotify(_settings.SoundVolume);

                UpdateValueLabel(_musicValue, _settings.MusicVolume);
                UpdateValueLabel(_soundValue, _settings.SoundVolume);
            }

            RefreshLanguage();
        }

        private void OnMusicChanged(float value)
        {
            if (_settings == null) return;

            _settings.MusicVolume = value;
            UpdateValueLabel(_musicValue, value);
        }

        private void OnSoundChanged(float value)
        {
            if (_settings == null) return;

            _settings.SoundVolume = value;
            UpdateValueLabel(_soundValue, value);
        }

        /// <summary>Текущий язык нельзя выбрать повторно: кнопка становится неактивной.</summary>
        private void RefreshLanguage()
        {
            if (_languageButtons == null) return;

            for (int i = 0; i < _languageButtons.Length; i++)
            {
                LanguageSettingsPanel.LanguageButton item = _languageButtons[i];
                if (item.Button == null) continue;

                item.Button.interactable = item.Code != YG2.lang;

                // Флаг показываем только если картинка назначена: пустой
                // белый квадрат рядом с названием выглядел бы как ошибка
                if (item.Flag != null) item.Flag.enabled = item.Flag.sprite != null;

                string code = item.Code;
                item.Button.onClick.RemoveAllListeners();
                item.Button.onClick.AddListener(() => ChooseLanguage(code));
            }
        }

        private void ChooseLanguage(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            // Совпадение с текущим языком — не ошибка, просто ничего не меняем.
            // Раньше здесь стоял `return` до всякой диагностики; если клик попадал
            // в перекрытую кнопку, код оказывался текущим, метод выходил молча,
            // и снаружи это выглядело как «кнопки не работают». Теперь пишем в Console.
            if (code == YG2.lang)
            {
                Debug.Log("[Settings] Язык уже выбран: " + code);
                return;
            }

            // Без перезагрузки сцены: она создаёт заново все сервисы, а AudioService
            // с DontDestroyOnLoad остаётся жить — отсюда вторая музыка.
            YG2.SwitchLanguage(code);

            // Явный вызов вместо надежды на подписку: плагин под #if UNITY_EDITOR
            // обнуляет YG2.onSwitchLang при инициализации, и подписка теряется.
            YG2LocaleBridge.Apply(code);

            // Тексты перечитает UiTextLabel по событию SelectedLocaleChanged.
            // Здесь только подсвечиваем активную кнопку.
            RefreshLanguage();
        }

        private static void UpdateValueLabel(TextMeshProUGUI label, float value)
        {
            if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void SetHidden(bool hide)
        {
            if (_hideWhileOpen != null && _hideWhileOpen.Length > 0)
            {
                foreach (GameObject go in _hideWhileOpen)
                {
                    if (go != null) go.SetActive(!hide);
                }
                return;
            }

            CubeSmasher.UI.HudVisibility.Toggle(hide);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveAllListeners();
            if (_soundSlider != null) _soundSlider.onValueChanged.RemoveAllListeners();
        }
    }
}
