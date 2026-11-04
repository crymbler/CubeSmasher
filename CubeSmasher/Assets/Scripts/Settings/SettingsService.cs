using CubeSmasher.Core.Interfaces;
using CubeSmasher.Infrastructure;
using UnityEngine;
using YG;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Громкость музыки и звука. Значения живут в сохранении YG2.
    /// Ползунок применяет громкость сразу, а запись на диск делается один раз —
    /// при закрытии окна.
    /// </summary>
    public class SettingsService
    {
        private readonly AudioService _audio;
        private readonly ISaveService _save;

        public SettingsService(AudioService audio, ISaveService save)
        {
            _audio = audio;
            _save = save;
            Apply();
        }

        public float MusicVolume
        {
            get => YG2.saves.musicVolume;
            set
            {
                YG2.saves.musicVolume = Mathf.Clamp01(value);
                _audio.SetMusic(value);
            }
        }

        public float SoundVolume
        {
            get => YG2.saves.soundVolume;
            set
            {
                YG2.saves.soundVolume = Mathf.Clamp01(value);
                _audio.SetSound(value);
            }
        }

        public void Apply()
        {
            _audio.ApplyVolumes(YG2.saves.musicVolume, YG2.saves.soundVolume);
        }

        public void Save()
        {
            _save.Save();
        }
    }
}
