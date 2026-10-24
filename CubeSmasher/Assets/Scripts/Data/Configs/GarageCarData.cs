using CubeSmasher.Settings;
using UnityEngine;

/// <summary>
/// Данные одной машины в гараже. Оставлены в глобальном пространстве имён,
/// потому что на этот тип ссылается интерфейс IGarageService.
/// </summary>
[System.Serializable]
public struct GarageCarData
{
    public string CarName;
    public int UnlockStage;
    public double IncomePerSecond;
    [TextArea] public string Description;

    [Header("Описание по языкам (K5). Пусто — берётся Description")]
    [TextArea] public string DescriptionRu;
    [TextArea] public string DescriptionEn;
    [TextArea] public string DescriptionTr;

    /// <summary>Описание на текущем языке.</summary>
    public string GetDescription() => Localization.Pick(DescriptionRu, DescriptionEn, DescriptionTr, Description);

    /// <summary>Баффы машины в свободной форме: "+5% к урону, +2 к радиусу".
    /// Показывается только у открытой машины. У старых ассетов пусто.</summary>
    [TextArea] public string Characteristics;

    public GameObject ModelPrefab; // 3D-модель машины
    public Sprite MiniIcon;        // 2D-иконка для интерфейса
}
