using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Звук движения ползунка слайдера: короткий мягкий "тик" на каждое заметное
    /// изменение значения (похоже на uppbeat "UI Slider Plip" — короткий щелчок
    /// без резкого звона). Звук процедурный, генерируется SliderPlip.CreateClip,
    /// внешний файл не нужен.
    ///
    /// Тики не играют на каждый кадр перетаскивания: это было бы слишком часто
    /// и слилось бы в шум. Звук срабатывает только когда значение сдвинулось
    /// на заметный шаг (по умолчанию 1/20 диапазона), поэтому на слайдере
    /// громкости из 20 делений получается 20 чётких тиков на весь путь.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class SliderTickSound : MonoBehaviour
    {
        [Tooltip("Доля диапазона слайдера между тиками. 0.05 = 20 тиков на весь путь")]
        [SerializeField, Range(0.01f, 0.5f)] private float _step = 0.05f;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;

        private Slider _slider;
        private float _lastTickValue;
        private bool _initialized;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(HandleChanged);
            _initialized = false;
        }

        private void OnDisable() => _slider.onValueChanged.RemoveListener(HandleChanged);

        private void HandleChanged(float value)
        {
            float range = Mathf.Max(0.0001f, _slider.maxValue - _slider.minValue);
            float normalized = (value - _slider.minValue) / range;

            if (!_initialized)
            {
                // Первый вызов случается при инициализации слайдера (Load в SoundPanel):
                // звук здесь не нужен, иначе тик играл бы при каждом открытии настроек.
                _lastTickValue = normalized;
                _initialized = true;
                return;
            }

            if (Mathf.Abs(normalized - _lastTickValue) < _step) return;

            _lastTickValue = normalized;
            Play();
        }

        private void Play()
        {
            if (!CubeSmasher.Infrastructure.ServiceLocator.Instance.TryGet(
                    out CubeSmasher.Infrastructure.AudioService audio)) return;

            audio.PlaySfxVaried(AudioClipLibrary.SliderTick, _volume);
        }
    }
}
