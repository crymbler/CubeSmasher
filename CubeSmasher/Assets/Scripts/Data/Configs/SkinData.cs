using UnityEngine;
using UnityEngine.Localization;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Один скин: набор цветов для деталей и цена.
    ///
    /// Скины в этой игре — расцветки кубов, а не отдельные модели: детали
    /// красятся ColorChanger, поэтому скин задаёт диапазон оттенков, а не меш.
    /// </summary>
    [System.Serializable]
    public struct SkinData
    {
        [Tooltip("Идентификатор для сохранения. Менять нельзя: по нему ищется купленный скин")]
        public string Id;

        [Tooltip("Ключ из String Table UI. Пусто — берётся DisplayName")]
        public LocalizedString LocalizedName;

        [Tooltip("Название, если ключ локализации не задан")]
        public string DisplayName;

        [Header("Цвет")]
        [Tooltip("Минимальный и максимальный оттенок 0..1. Скин может задавать один цвет: одинаковые значения")]
        [Range(0f, 1f)] public float HueMin;
        [Range(0f, 1f)] public float HueMax;

        [Tooltip("Насыщенность и яркость деталей этого скина")]
        [Range(0f, 1f)] public float Saturation;
        [Range(0f, 1f)] public float Brightness;

        [Header("Открытие")]
        [Tooltip("С какого уровня стадии скин появляется в магазине")]
        public int UnlockStage;

        [Tooltip("Цена в мягкой валюте (монеты). Если скин премиальный, цена берётся из HardPrice")]
        public double SoftPrice;

        [Tooltip("Цена в твёрдой валюте. Работает только при включённом _hardCurrencyEnabled")]
        public int HardPrice;

        [Tooltip("Премиальный скин: покупается только за твёрдую валюту")]
        public bool IsPremium;

        [Tooltip("Скин доступен всем сразу, без покупки")]
        public bool IsDefault;

        public string GetName() => LocalizedName != null && !LocalizedName.IsEmpty
            ? LocalizedName.GetLocalizedString()
            : DisplayName;

        /// <summary>Случайный цвет в диапазоне скина, HSV как у кислотной палитры.</summary>
        public Color GetColor()
        {
            float hue = HueMin >= HueMax ? HueMin : Random.Range(HueMin, HueMax);
            return Color.HSVToRGB(hue, Saturation, Brightness);
        }
    }
}
