using UnityEngine;
using UnityEngine.Audio;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Создаёт AudioService и регистрирует его в контейнере.
    ///
    /// Своих подписок на игровые события не делает: звук открытия машины
    /// проигрывает GarageView через этот же сервис. Иначе на одно событие
    /// приходилось бы два звука.
    /// </summary>
    public class AudioInitializer : IGameInitializer
    {
        private readonly AudioMixer _mixer;
        private readonly AudioClip _music;

        public AudioInitializer(AudioMixer mixer, AudioClip music)
        {
            _mixer = mixer;
            _music = music;
        }

        public void Initialize()
        {
            var audio = new AudioService(_mixer);
            ServiceLocator.Instance.Register(audio);

            if (_music != null) audio.PlayMusic(_music);
        }
    }
}
