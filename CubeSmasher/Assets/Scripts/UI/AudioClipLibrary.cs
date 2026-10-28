using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Общие звуки интерфейса, которые не стоит таскать как файлы по сцене.
    ///
    /// Click, HammerHit и Unlock — это Assets/Audio/Sounds/*.mp3, подключённые
    /// через Tools > CubeSmasher > Setup Audio (он же пишет пути в это хранилище).
    /// SliderTick — процедурный клип, который ни один дизайнер не рисовал в Audacity:
    /// готового бесплатного аналога "UI Slider Plip" с uppbeat в проекте нет,
    /// а звук нужен прямо сейчас, поэтому он сгенерирован кодом (SliderPlip.CreateClip).
    /// Если позже появится обычный файл, его можно подставить сюда же.
    /// </summary>
    public static class AudioClipLibrary
    {
        public static AudioClip Click;
        public static AudioClip HammerHit;
        public static AudioClip Unlock;

        private static AudioClip _sliderTick;
        public static AudioClip SliderTick => _sliderTick ??= SliderPlip.CreateClip();
    }
}
