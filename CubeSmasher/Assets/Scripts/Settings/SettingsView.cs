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

            // Клики привязываются здесь И в Refresh: если окно когда-либо
            // включается без вызова Open, кнопки иначе остались бы без
            // обработчиков и молчали бы — без единой ошибки в консоли
            BindLanguageButtons();
        }

        /// <summary>Передать сервис настроек. Вызывается из UIWiringInitializer.</summary>
        public void Initialize(SettingsService settings)
        {
            _settings = settings;
            Refresh();
        }

        /// <summary>
        /// Окно сейчас открыто. Нужно, чтобы не включить паузу дважды:
        /// иначе одно закрытие её не снимет и игра останется стоять.
        /// </summary>
        public bool IsOpen => gameObject.activeSelf;

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
        /// <summary>
        /// Привязать обработчики к кнопкам языка.
        ///
        /// Здесь же чинится самая дорогая поломка этого экрана: если Graphic
        /// у кнопки выключен (m_Enabled = 0), он не участвует в raycast,
        /// и клик до Button не доходит вообще. Снаружи это выглядит как
        /// «кнопка есть, но не работает», и в Console нет ни строчки.
        /// Поэтому графика принудительно включается и делается кликабельной.
        /// </summary>
        private void BindLanguageButtons()
        {
            if (_languageButtons == null || _languageButtons.Length == 0)
            {
                Debug.LogError("[Settings] Список кнопок языка пуст: назначьте " +
                               "_languageButtons в инспекторе Settings_Screen.");
                return;
            }

            for (int i = 0; i < _languageButtons.Length; i++)
            {
                LanguageSettingsPanel.LanguageButton item = _languageButtons[i];

                if (item.Button == null)
                {
                    Debug.LogError("[Settings] У кнопки языка '" + item.Code +
                                   "' не назначен Button.");
                    continue;
                }

                if (string.IsNullOrEmpty(item.Code))
                {
                    Debug.LogError("[Settings] Пустой код языка у кнопки " +
                                   item.Button.name + ".");
                    continue;
                }

                EnsureClickable(item.Button);

                // Флаг показываем только если картинка назначена: пустой
                // белый квадрат рядом с названием выглядел бы как ошибка
                if (item.Flag != null) item.Flag.enabled = item.Flag.sprite != null;

                string code = item.Code;
                item.Button.onClick.RemoveAllListeners();
                item.Button.onClick.AddListener(() => ChooseLanguage(code));
            }
        }

        /// <summary>
        /// Сделать кнопку реально кликабельной. Выключенный Graphic не попадает
        /// в список целей raycast, а пустой TargetGraphic ломает Selectable.
        /// </summary>
        private static void EnsureClickable(Button button)
        {
            Graphic graphic = button.targetGraphic;

            if (graphic == null) graphic = button.GetComponent<Graphic>();

            if (graphic == null)
            {
                Debug.LogError("[Settings] У кнопки " + button.name +
                               " нет Graphic: клик невозможен.");
                return;
            }

            if (!graphic.enabled) graphic.enabled = true;
            if (!graphic.raycastTarget) graphic.raycastTarget = true;

            // Пустой targetGraphic ломает переходы состояний Selectable
            if (button.targetGraphic == null) button.targetGraphic = graphic;
        }

        /// <summary>Подсветить текущий язык: его кнопка становится неактивной.</summary>
        private void RefreshLanguage()
        {
            if (_languageButtons == null) return;

            for (int i = 0; i < _languageButtons.Length; i++)
            {
                LanguageSettingsPanel.LanguageButton item = _languageButtons[i];
                if (item.Button == null) continue;

                item.Button.interactable = item.Code != YG2.lang;
            }
        }

        private void ChooseLanguage(string code)
        {
            // Лог на входе: это единственный способ отличить
            // «клик не дошёл до обработчика» от «дошёл, но не переключил»
            Debug.Log("[Settings] Нажата кнопка языка: " + code + " (текущий: " + YG2.lang + ")");

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
