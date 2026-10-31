using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Один вариант косметики: молот или тарелка.
    ///
    /// Молоты различаются моделью (свой префаб), тарелки — тем же мешем
    /// с другим материалом. Поэтому здесь есть и ModelPrefab, и Material:
    /// если задан ModelPrefab, ставится модель; если только Material,
    /// меш остаётся текущий, а меняется материал.
    /// </summary>
    [System.Serializable]
    public struct CosmeticItem
    {
        [Tooltip("Идентификатор для сохранения. Менять нельзя: по нему ищется купленное")]
        public string Id;

        [Tooltip("Ключ названия из String Table UI")]
        public string NameKey;

        [Header("Внешний вид")]
        [Tooltip("Префаб модели. Для тарелок можно оставить пустым и задать только Material")]
        public GameObject ModelPrefab;

        [Tooltip("Материал. Применяется, если ModelPrefab пуст: меняется цвет, меш остаётся")]
        public Material Material;

        [Header("Открытие")]
        [Tooltip("С какого уровня стадии предмет появляется в магазине")]
        public int UnlockStage;

        [Tooltip("Доступен всем сразу, без покупки")]
        public bool IsDefault;

        [Header("Цена")]
        [Tooltip("Включено — цена в монетах игры (SoftPrice)")]
        public bool ForCoins;

        [Tooltip("Цена в монетах. Работает при включённом ForCoins")]
        public double SoftPrice;

        [Tooltip("Id товара из консоли разработчика Яндекс.Игр для покупки за яны. " +
                 "Пусто — предмет нельзя купить за донат")]
        public string PurchaseId;

        [Tooltip("Премиальный предмет: покупается только за яны")]
        public bool IsPremium;

        /// <summary>Название на текущем языке.</summary>
        public string GetName() =>
            string.IsNullOrEmpty(NameKey) ? Id : UiText.Get(NameKey);
    }

    /// <summary>
    /// Группа косметики: молоты, тарелки. Внутри три варианта —
    /// два за монеты и один премиальный, как в макете магазина.
    /// </summary>
    [System.Serializable]
    public struct CosmeticGroup
    {
        [Tooltip("Идентификатор группы для сохранения: hammer, dish")]
        public string Id;

        [Tooltip("Ключ названия группы из String Table UI")]
        public string TitleKey;

        [Tooltip("Ровно три варианта: два за монеты, один премиальный")]
        public CosmeticItem[] Items;

        public string GetTitle() =>
            string.IsNullOrEmpty(TitleKey) ? Id : UiText.Get(TitleKey);
    }
}
