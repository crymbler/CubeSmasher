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

    public GameObject ModelPrefab; // 3D-модель машины
    public Sprite MiniIcon;        // 2D-иконка для интерфейса
}
