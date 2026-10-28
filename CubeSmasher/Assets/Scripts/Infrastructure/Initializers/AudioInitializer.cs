using UnityEngine;
using UnityEngine.Audio;
using CubeSmasher.UI;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Создаёт AudioService, регистрирует его в контейнере и заполняет
    /// AudioClipLibrary общими звуками (клик, удар молота, открытие машины),
    /// чтобы ButtonClickSound и другие компоненты могли их использовать
    /// без ручного назначения клипа на каждой кнопке.
    ///
    /// Своих подписок на игровые события не делает: звук открытия машины
    /// проигрывает GarageView через этот же сервис. Иначе на одно событие
    /// приходилось бы два звука.
    /// </summary>
    public class AudioInitializer : IGameInitializer
    {
        private readonly AudioMixer _mixer;
        private readonly AudioClip _music;
        private readonly AudioClip _click;
        private readonly AudioClip _hammerHit;
        private readonly AudioClip _unlock;
        private readonly AudioClip _split;

        public AudioInitializer(AudioMixer mixer, AudioClip music,
                                AudioClip click, AudioClip hammerHit, AudioClip unlock,
                                AudioClip split)
        {
            _mixer = mixer;
            _music = music;
            _click = click;
            _hammerHit = hammerHit;
            _unlock = unlock;
            _split = split;
        }

        public void Initialize()
        {
            var audio = new AudioService(_mixer);
            ServiceLocator.Instance.Register(audio);

            if (_music != null) audio.PlayMusic(_music);

            AudioClipLibrary.Click = _click;
            AudioClipLibrary.HammerHit = _hammerHit;
            AudioClipLibrary.Unlock = _unlock;
            AudioClipLibrary.Split = _split;
        }
    }
}
