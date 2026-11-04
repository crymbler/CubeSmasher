using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Экран настроек: громкость музыки, громкость звуков, выбор языка.
    /// Подписки живут в OnEnable/OnDisable. Громкость применяется сразу,
    /// запись на диск идёт один раз при закрытии окна.
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

        public event Action Closed;

        private SettingsService _settings;

        public void Initialize(SettingsService settings)
        {
            _settings = settings;
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            _soundSlider.onValueChanged.AddListener(OnSoundChanged);

            for (int i = 0; i < _languageButtons.Length; i++)
            {
                string code = _languageButtons[i].Code;
                _languageButtons[i].Button.onClick.AddListener(() => ChooseLanguage(code));
            }

            Refresh();
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
            _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            _soundSlider.onValueChanged.RemoveListener(OnSoundChanged);

            for (int i = 0; i < _languageButtons.Length; i++)
            {
                _languageButtons[i].Button.onClick.RemoveAllListeners();
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            SetHidden(true);
        }

        public void Close()
        {
            _settings.Save();

            gameObject.SetActive(false);
            SetHidden(false);
            Closed?.Invoke();
        }

        private void Refresh()
        {
            _musicSlider.SetValueWithoutNotify(_settings.MusicVolume);
            _soundSlider.SetValueWithoutNotify(_settings.SoundVolume);

            _musicValue.text = Percent(_settings.MusicVolume);
            _soundValue.text = Percent(_settings.SoundVolume);

            RefreshLanguageButtons();
        }

        private void OnMusicChanged(float value)
        {
            _settings.MusicVolume = value;
            _musicValue.text = Percent(value);
        }

        private void OnSoundChanged(float value)
        {
            _settings.SoundVolume = value;
            _soundValue.text = Percent(value);
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

        private void RefreshLanguageButtons()
        {
            for (int i = 0; i < _languageButtons.Length; i++)
            {
                LanguageSettingsPanel.LanguageButton item = _languageButtons[i];
                item.Button.interactable = item.Code != YG2.lang;

                if (item.Flag != null) item.Flag.enabled = item.Flag.sprite != null;
            }
        }

        private void ChooseLanguage(string code)
        {
            if (code == YG2.lang) return;

            YG2.SwitchLanguage(code);
            YG2LocaleBridge.Apply(code);
            RefreshLanguageButtons();
        }

        private void SetHidden(bool hide)
        {
            if (_hideWhileOpen != null && _hideWhileOpen.Length > 0)
            {
                for (int i = 0; i < _hideWhileOpen.Length; i++)
                {
                    _hideWhileOpen[i].SetActive(!hide);
                }
                return;
            }

            CubeSmasher.UI.HudVisibility.Toggle(hide);
        }
    }
}
