using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Звук клика на любой Button. Проигрывает clip через AudioService с небольшим
    /// случайным питчем, чтобы одинаковые клики звучали не совсем одинаково.
    ///
    /// Если AudioClip не назначен в инспекторе, используется общий клик из
    /// AudioClipLibrary (см. Tools > CubeSmasher > Setup Audio).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonClickSound : MonoBehaviour
    {
        [Tooltip("Пусто — берётся общий звук клика из AudioClipLibrary")]
        [SerializeField] private AudioClip _clip;

        [Tooltip("Разброс питча: 0.05 даёт ±5% к высоте звука")]
        [SerializeField, Range(0f, 0.3f)] private float _pitchJitter = 0.05f;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable() => _button.onClick.AddListener(Play);
        private void OnDisable() => _button.onClick.RemoveListener(Play);

        private void Play()
        {
            if (!CubeSmasher.Infrastructure.ServiceLocator.Instance.TryGet(
                    out CubeSmasher.Infrastructure.AudioService audio)) return;

            AudioClip clip = _clip != null ? _clip : AudioClipLibrary.Click;
            audio.PlaySfxWithJitter(clip, _pitchJitter);
        }
    }
}
