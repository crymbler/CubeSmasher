using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Список скинов и флаг твёрдой валюты.
    ///
    /// _hardCurrencyEnabled управляет всей премиальной частью: пока он выключен,
    /// премиальные скины не показываются в магазине, а твёрдая валюта не тратится.
    /// Это позволяет выпустить игру с одними монетами и включить донат позже,
    /// не меняя код магазина.
    /// </summary>
    [CreateAssetMenu(fileName = "SkinConfig", menuName = "CubeSmasher/Skin Config")]
    public class SkinConfig : ScriptableObject
    {
        [Header("Твёрдая валюта")]
        [Tooltip("Пока выключено, премиальные скины скрыты и твёрдая валюта не тратится")]
        public bool HardCurrencyEnabled;

        [Tooltip("Сколько твёрдой валюты выдаётся новому игроку. 0 — не выдавать")]
        public int StartingHardCurrency;

        [Header("Скины")]
        [Tooltip("Первый скин в списке используется по умолчанию у нового игрока")]
        public SkinData[] Skins;
    }
}
