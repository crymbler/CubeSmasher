using UnityEngine;
using UnityEngine.Audio;
using YG;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Звук игры: музыка в цикле и короткие эффекты через пул AudioSource.
    ///
    /// Громкость берётся из сохранения YG2 (musicVolume, soundVolume) и уходит
    /// в AudioMixer. Параметры в Master.mixer называются Music и UI — этот сервис
    /// использует те же имена, что и SoundPanel, иначе ползунки в настройках
    /// двигали бы не те группы.
    ///
    /// SFX играются через пул из нескольких AudioSource, а не через один общий.
    /// AudioSource.pitch — это свойство самого источника, оно действует на все
    /// звуки, что сейчас на нём играют. Если несколько эффектов перекрываются
    /// во времени (быстрые клики, перетаскивание слайдера), один общий источник
    /// на лету поменял бы питч уже играющему звуку. Пул даёт каждому эффекту
    /// свою дорожку, поэтому разный питч не мешает соседним звукам.
    ///
    /// Без микшера громкость ставится напрямую у AudioSource, поэтому сервис
    /// работает и в сцене, где микшер не назначен.
    /// </summary>
    public class AudioService
    {
        public const string MusicParam = "Music";
        public const string SoundParam = "UI";
        private const float MutedDecibels = -80f;
        private const int SfxPoolSize = 6;

        /// <summary>Имя объекта в сцене: по нему находим уже живой хост.</summary>
        private const string HostName = "AudioService";

        private readonly AudioSource _music;
        private readonly AudioSource[] _sfxPool;
        private readonly AudioMixer _mixer;
        private readonly float _sfxBaseVolume;
        private int _nextSfxIndex;

        public AudioService(AudioMixer mixer)
        {
            _mixer = mixer;

            // Хост живёт между сценами (DontDestroyOnLoad). Если Bootstrapper создаётся
            // заново после перезагрузки сцены, новый AudioService создал бы ВТОРОЙ такой
            // объект — и вторая музыка играла бы поверх первой. Поэтому ищем живой хост
            // и переиспользуем его источники вместо создания новых.
            var host = GameObject.Find(HostName);

            if (host != null)
            {
                AudioSource[] existing = host.GetComponents<AudioSource>();

                _music = existing.Length > 0 ? existing[0] : host.AddComponent<AudioSource>();
                _music.loop = true;
                _music.playOnAwake = false;
                _music.spatialBlend = 0f;

                _sfxPool = new AudioSource[SfxPoolSize];
                for (int i = 0; i < _sfxPool.Length; i++)
                {
                    int index = i + 1;
                    _sfxPool[i] = index < existing.Length
                        ? existing[index]
                        : host.AddComponent<AudioSource>();

                    _sfxPool[i].playOnAwake = false;
                    _sfxPool[i].spatialBlend = 0f;
                }
            }
            else
            {
                host = new GameObject(HostName);
                Object.DontDestroyOnLoad(host);

                _music = host.AddComponent<AudioSource>();
                _music.loop = true;
                _music.playOnAwake = false;
                _music.spatialBlend = 0f;

                _sfxPool = new AudioSource[SfxPoolSize];
                for (int i = 0; i < _sfxPool.Length; i++)
                {
                    var source = host.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    _sfxPool[i] = source;
                }
            }

            AudioMixerGroup musicGroup = FindGroup("Music");
            AudioMixerGroup sfxGroup = FindGroup("UI") ?? FindGroup("Sound");
            if (musicGroup != null) _music.outputAudioMixerGroup = musicGroup;

            for (int i = 0; i < _sfxPool.Length; i++)
            {
                if (sfxGroup != null) _sfxPool[i].outputAudioMixerGroup = sfxGroup;
            }

            // Без микшера громкость SFX хранится тут и применяется к каждому
            // источнику пула перед проигрыванием (volume у PlayOneShot — множитель).
            _sfxBaseVolume = 1f;

            ApplyVolumes(YG2.saves.musicVolume, YG2.saves.soundVolume);

            // Bootstrapper создаёт AudioService раньше, чем YG2 загрузит сохранение.
            // Пока не загружено, в микшер уходят значения по умолчанию (0.5).
            YG2.onGetSDKData -= ApplySavedVolumes;
            YG2.onGetSDKData += ApplySavedVolumes;
        }

        private void ApplySavedVolumes()
        {
            ApplyVolumes(YG2.saves.musicVolume, YG2.saves.soundVolume);
        }

        public void SetMusic(float volume)
        {
            if (_mixer != null) _mixer.SetFloat(MusicParam, ToDecibels(volume));
            else _music.volume = Mathf.Clamp01(volume);
        }

        public void SetSound(float volume)
        {
            if (_mixer != null) _mixer.SetFloat(SoundParam, ToDecibels(volume));
            else SetSfxVolume(Mathf.Clamp01(volume));
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

        /// <summary>Короткий эффект без изменения высоты тона.</summary>
        public void PlaySfx(AudioClip clip, float volume = 1f) => PlaySfx(clip, volume, 1f);

        /// <summary>
        /// Короткий эффект со случайным питчем в диапазоне [1-jitter, 1+jitter].
        /// Небольшой разброс (0.05-0.08) делает повторяющиеся звуки (клик, удар,
        /// тик слайдера) менее монотонными без заметного искажения тона.
        /// </summary>
        public void PlaySfxWithJitter(AudioClip clip, float pitchJitter, float volume = 1f)
        {
            float pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            PlaySfx(clip, volume, pitch);
        }

        /// <summary>
        /// Единый диапазон питча для всех повторяющихся звуков: 0.92–1.08.
        /// Одинаковый клик, удар или раскол звучат чуть по-разному, но не уходят
        /// в писк или бас.
        /// </summary>
        public const float PitchMin = 0.92f;
        public const float PitchMax = 1.08f;

        public void PlaySfxVaried(AudioClip clip, float volume = 1f)
        {
            float pitch = Random.Range(PitchMin, PitchMax);
            PlaySfx(clip, volume, pitch);
        }

        public void PlaySfx(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _sfxPool.Length == 0) return;

            AudioSource source = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Length;

            // volume у PlayOneShot — множитель к базовой громкости источника
            // (source.volume), которую выставляет SetSfxVolume без микшера.
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
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
                SetSfxVolume(Mathf.Clamp01(soundVolume));
            }
        }

        private void SetSfxVolume(float volume)
        {
            // volume у PlayOneShot — множитель к этому полю не относится: оно задаёт
            // базовую громкость источника. Храним множитель отдельно через поле класса,
            // чтобы не плодить лишние состояния на каждом источнике пула.
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                _sfxPool[i].volume = volume;
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
