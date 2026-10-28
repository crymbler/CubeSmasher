using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Процедурный звук "плип" для слайдера: короткий тональный щелчок с быстрым
    /// затуханием, без резкого звона и без шума. По ощущению близко к типичным
    /// референсам вроде uppbeat "UI Slider Plip" — короткий мягкий тик, а не
    /// длинный звон колокольчика.
    ///
    /// Звук строится из двух слоёв:
    ///  - основной тон на SliderPlip.Frequency с экспоненциальным затуханием —
    ///    даёт ощутимый, но мягкий "тук";
    ///  - короткий высокочастотный "щелчок" первые несколько миллисекунд —
    ///    даёт ощущение чёткости атаки, без него звук был бы глухим и смазанным.
    /// </summary>
    public static class SliderPlip
    {
        private const int SampleRate = 44100;
        private const float Duration = 0.06f;
        private const float Frequency = 1500f;
        private const float ClickFrequency = 5000f;
        private const float ClickDuration = 0.006f;

        public static AudioClip CreateClip()
        {
            int samples = Mathf.CeilToInt(SampleRate * Duration);
            var data = new float[samples];

            float tonePhase = 0f;
            float clickPhase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;

                // Основной тон: быстрое экспоненциальное затухание даёт короткий "тук",
                // а не выдержанную ноту.
                float toneEnvelope = Mathf.Exp(-t * 55f);
                tonePhase += 2f * Mathf.PI * Frequency / SampleRate;
                float tone = Mathf.Sin(tonePhase) * toneEnvelope;

                // Щелчок атаки: короче и тише тона, затухает почти мгновенно.
                float click = 0f;
                if (t < ClickDuration)
                {
                    float clickEnvelope = 1f - (t / ClickDuration);
                    clickPhase += 2f * Mathf.PI * ClickFrequency / SampleRate;
                    click = Mathf.Sin(clickPhase) * clickEnvelope;
                }

                data[i] = tone * 0.5f + click * 0.3f;
            }

            var clip = AudioClip.Create("SliderPlip", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
