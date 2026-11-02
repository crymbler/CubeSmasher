using CubeSmasher.Core.Interfaces;
using CubeSmasher.Infrastructure;
using UnityEngine;
using YG;

namespace CubeSmasher.Settings
{
    /// <summary>
    /// Громкость музыки и звука. Отдельный сервис, потому что трогает сразу три вещи:
    /// сохранение (YG2.saves), микшер (AudioService) и UI.
    ///
    /// Значения живут в сохранении YG2 (musicVolume / soundVolume), поэтому переживают
    /// перезапуск и синхронизируются с облаком Яндекс.Игр. Меняются только через
    /// свойства: каждое изменение сразу применяется к микшеру и сохраняется.
    /// </summary>
    public class SettingsService
    {
        private readonly AudioService _audio;
        private readonly ISaveService _saveService;

        public SettingsService(AudioService audio, ISaveService saveService)
        {
            _audio = audio;
            _saveService = saveService;

            Apply();
        }

        /// <summary>Громкость музыки 0..1.</summary>
        public float MusicVolume
        {
            get => YG2.saves.musicVolume;
            set
            {
                YG2.saves.musicVolume = Mathf.Clamp01(value);
                Apply();
                _saveService.Save();
            }
        }

        /// <summary>Громкость звуков 0..1.</summary>
        public float SoundVolume
        {
            get => YG2.saves.soundVolume;
            set
            {
                YG2.saves.soundVolume = Mathf.Clamp01(value);
                Apply();
                _saveService.Save();
            }
        }

        /// <summary>Применить сохранённые значения к микшеру.</summary>
        public void Apply()
        {
            if (_audio != null) _audio.ApplyVolumes(YG2.saves.musicVolume, YG2.saves.soundVolume);
        }
    }
}
