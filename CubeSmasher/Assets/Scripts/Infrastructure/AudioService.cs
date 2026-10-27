using UnityEngine;
using UnityEngine.Audio;
using YG;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Звук игры: музыка в цикле и короткие эффекты через PlayOneShot.
    ///
    /// Громкость берётся из сохранения YG2 (musicVolume, soundVolume) и уходит
    /// в AudioMixer. Параметры в Master.mixer называются Music и UI — этот сервис
    /// использует те же имена, что и SoundPanel, иначе ползунки в настройках
    /// двигали бы не те группы.
    ///
    /// Без микшера громкость ставится напрямую у AudioSource, поэтому сервис
    /// работает и в сцене, где микшер не назначен.
    /// </summary>
    public class AudioService
    {
        public const string MusicParam = "Music";
        public const string SoundParam = "UI";
        private const float MutedDecibels = -80f;

        private readonly AudioSource _music;
        private readonly AudioSource _sfx;
        private readonly AudioMixer _mixer;

        public AudioService(AudioMixer mixer)
        {
            _mixer = mixer;

            var host = new GameObject("AudioService");
            Object.DontDestroyOnLoad(host);

            _music = host.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;

            _sfx = host.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            if (_mixer != null)
            {
                _music.outputAudioMixerGroup = FindGroup("Music");
                _sfx.outputAudioMixerGroup = FindGroup("UI") ?? FindGroup("Sound");
            }

            ApplyVolumes(YG2.saves.musicVolume, YG2.saves.soundVolume);
        }

        /// <summary>Включить музыку. Повторный вызов с тем же клипом ничего не меняет.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;

            _music.clip = clip;
            _music.Play();
        }

        public void StopMusic() => _music.Stop();

        /// <summary>Короткий эффект. Громкость задаётся ползунком звука.</summary>
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _sfx.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// Применяет громкость 0..1. Значения переводятся в децибелы,
        /// ноль глушит группу полностью: Log10(0) даёт минус бесконечность.
        /// </summary>
        public void ApplyVolumes(float musicVolume, float soundVolume)
        {
            if (_mixer != null)
            {
                _mixer.SetFloat(MusicParam, ToDecibels(musicVolume));
                _mixer.SetFloat(SoundParam, ToDecibels(soundVolume));
            }
            else
            {
                _music.volume = Mathf.Clamp01(musicVolume);
                _sfx.volume = Mathf.Clamp01(soundVolume);
            }
        }

        private AudioMixerGroup FindGroup(string name)
        {
            if (_mixer == null) return null;

            AudioMixerGroup[] groups = _mixer.FindMatchingGroups(name);
            return groups != null && groups.Length > 0 ? groups[0] : null;
        }

        private static float ToDecibels(float linear) =>
            linear <= 0.0001f ? MutedDecibels : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
    }
}
