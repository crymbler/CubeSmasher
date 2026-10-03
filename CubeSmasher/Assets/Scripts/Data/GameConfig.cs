using UnityEngine;

[System.Serializable]
public struct GarageCarData
{
    public string CarName;
    public int UnlockStage;
    public double IncomePerSecond;
    [TextArea] public string Description;

    public GameObject ModelPrefab; // 3D-префаб для подиума
    public Sprite MiniIcon;        // 2D-картинка для нижней ленты
}

[CreateAssetMenu(fileName = "GameConfig", menuName = "Tycoon/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Визуал деталей")]
    [Tooltip("Перетащи сюда все модели (Mesh)")]
    public CarPart[] PartPrefabs;

    [Header("Молот: Урон")]
    public float BaseDamage = 10f;
    public float DamageMultiplier = 1.20f; // Урон растет на 20%
    public double BaseDamageCost = 50;
    public float DamageCostMultiplier = 1.35f; // Цена растет на 35%

    [Header("Молот: Радиус сплеша")]
    public float BaseRadius = 1.0f;
    public float RadiusStep = 0.15f; // Радиус растет линейно
    public double BaseRadiusCost = 150;
    public float RadiusCostMultiplier = 1.50f;

    [Header("Молот: Критический удар")]
    public float BaseCritChance = 0.01f; // 1%
    public float CritChanceStep = 0.01f; // +1% за уровень
    public float MaxCritChance = 0.35f; // Лимит 35%
    public float CritMultiplier = 5f; // Урон х5

    [Header("Настройки разрушения (Размеры и Шансы)")]
    [Tooltip("Стартовый размер деталей при падении сверху (Gen 0)")]
    public float InitialPartScale = 1f;

    [Tooltip("Базовый шанс деления детали на осколки (от 0 до 1)")]
    [Range(0f, 1f)]
    public float BaseSplitChance = 0.85f;

    [Tooltip("Насколько меньше становятся осколки при взрыве (0.5 = в 2 раза)")]
    public float SplitScaleMultiplier = 0.5f;

    [Header("Экономика деталей (Стадии)")]
    public float BasePartHP = 20f;
    public float PartHPMultiplier = 1.45f;
    public double BasePartReward = 5;
    public float PartRewardMultiplier = 1.25f;

    [Header("Спавн и Лимиты пула")]
    [Tooltip("Радиус разброса деталей при появлении сверху")]
    public float SpawnRadius = 4f;

    [Tooltip("Размер пула деталей (Лимит объектов на сцене)")]
    public int PoolCapacity = 250;

    [Tooltip("Сколько больших деталей (Gen 0) падает за один раз")]
    public int StartPartsPerWave = 1;

    [Tooltip("Максимальное количество делений (2 = 3 стадии, 3 = 4 стадии)")]
    public int MaxGenerations = 3;

    [Header("Гараж и Пассивный доход")]
    public GarageCarData[] GarageCars;

    // --- МЕТОДЫ РАСЧЕТА (Используют формулы из GDD) ---

    public float GetDamage(int level) => BaseDamage * Mathf.Pow(DamageMultiplier, level - 1);
    public double GetDamageCost(int level) => BaseDamageCost * Mathf.Pow(DamageCostMultiplier, level - 1);

    public float GetRadius(int level) => BaseRadius + RadiusStep * (level - 1);
    public double GetRadiusCost(int level) => BaseRadiusCost * Mathf.Pow(RadiusCostMultiplier, level - 1);

    public float GetCritChance(int level) => Mathf.Clamp(BaseCritChance + CritChanceStep * (level - 1), 0, MaxCritChance);
}