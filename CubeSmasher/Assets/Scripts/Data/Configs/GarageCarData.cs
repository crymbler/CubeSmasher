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

    [Header("Название (локализация)")]
    [Tooltip("Ключ из String Table UI. Если пусто, берётся CarName")]
    public UnityEngine.Localization.LocalizedString LocalizedName;

    /// <summary>Название на текущем языке.</summary>
    public string GetName() => LocalizedName != null && !LocalizedName.IsEmpty
        ? LocalizedName.GetLocalizedString()
        : CarName;

    [Header("Описание (локализация)")]
    [Tooltip("Ключ из String Table UI. Если пусто, берётся Description")]
    public UnityEngine.Localization.LocalizedString LocalizedDescription;

    /// <summary>Описание на текущем языке.</summary>
    public string GetDescription() => LocalizedDescription != null && !LocalizedDescription.IsEmpty
        ? LocalizedDescription.GetLocalizedString()
        : Description;

    /// <summary>Баффы машины в свободной форме: "+5% к урону, +2 к радиусу".
    /// Показывается только у открытой машины. У старых ассетов пусто.</summary>
    [TextArea] public string Characteristics;

    public GameObject ModelPrefab; // 3D-модель машины
    public Sprite MiniIcon;        // 2D-иконка для интерфейса
}
