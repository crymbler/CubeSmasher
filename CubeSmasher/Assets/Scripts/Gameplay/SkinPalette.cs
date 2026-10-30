using UnityEngine;

namespace CubeSmasher.Gameplay
{
    /// <summary>
    /// Палитра активного скина: диапазон оттенков и фиксированные насыщенность с яркостью.
    ///
    /// Отдельный тип, потому что ColorChanger живёт на объектах из пула и не должен
    /// ходить в ServiceLocator при каждой перекраске: деталей на столе десятки,
    /// и поиск сервиса на каждую был бы лишней работой.
    /// </summary>
    public readonly struct SkinPalette
    {
        public readonly float HueMin;
        public readonly float HueMax;
        public readonly float Saturation;
        public readonly float Brightness;

        public SkinPalette(float hueMin, float hueMax, float saturation, float brightness)
        {
            HueMin = hueMin;
            HueMax = hueMax;
            Saturation = saturation;
            Brightness = brightness;
        }

        /// <summary>Случайный цвет в диапазоне скина.</summary>
        public Color Sample()
        {
            float hue = HueMin >= HueMax ? HueMin : Random.Range(HueMin, HueMax);
            return Color.HSVToRGB(hue, Saturation, Brightness);
        }
    }

    /// <summary>
    /// Текущая палитра скина. Ставится SkinService при старте и при смене скина.
    /// null — скин не выбран, ColorChanger красит детали как раньше, случайным HSV.
    /// </summary>
    public static class SkinPaletteState
    {
        public static SkinPalette? Current;
    }
}
