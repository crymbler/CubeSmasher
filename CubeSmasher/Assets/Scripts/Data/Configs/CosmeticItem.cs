using UnityEngine;
using UnityEngine.Localization;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Один вариант косметики: молот или тарелка.
    ///
    /// Название задаётся ДВУМЯ полями, и это главное, что нужно понять:
    ///  - LocalizedName — пикер ключа из таблицы UI. Выбирается мышкой в инспекторе,
    ///    и название само переводится на язык игрока;
    ///  - Name — обычный текст, показывается, если ключ не выбран.
    ///
    /// Так же сделано в GarageCarData: сначала переводимое, потом запасной текст.
    /// В LocalizedName НЕЛЬЗЯ писать сам текст — только выбирать ключ из списка.
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

        [Header("Название")]
        [Tooltip("Ключ из таблицы UI. Выберите из выпадающего списка — переведётся сам")]
        public LocalizedString LocalizedName;

        [Tooltip("Текст, если ключ не выбран. На других языках переводиться не будет")]
        public string Name;

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

        /// <summary>
        /// Название на текущем языке. Порядок: ключ перевода, потом обычный текст,
        /// потом Id — чтобы в магазине никогда не было пустой карточки.
        /// </summary>
        public string GetName()
        {
            if (LocalizedName != null && !LocalizedName.IsEmpty)
            {
                string translated = LocalizedName.GetLocalizedString();
                if (!string.IsNullOrEmpty(translated)) return translated;
            }

            return string.IsNullOrEmpty(Name) ? Id : Name;
        }
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

        [Header("Название")]
        [Tooltip("Ключ из таблицы UI. Выберите из выпадающего списка")]
        public LocalizedString LocalizedTitle;

        [Tooltip("Текст, если ключ не выбран")]
        public string Title;

        [Tooltip("Ровно три варианта: два за монеты, один премиальный")]
        public CosmeticItem[] Items;

        public string GetTitle()
        {
            if (LocalizedTitle != null && !LocalizedTitle.IsEmpty)
            {
                string translated = LocalizedTitle.GetLocalizedString();
                if (!string.IsNullOrEmpty(translated)) return translated;
            }

            return string.IsNullOrEmpty(Title) ? Id : Title;
        }
    }
}
