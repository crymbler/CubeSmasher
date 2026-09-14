using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class Bootstrapper : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private HammerCaster _hammerCaster;

    [Header("Пул и Префабы")]
    [SerializeField] private CarPart _carPartPrefab;
    [SerializeField] private Transform _poolContainer;

    [Header("UI Views")]
    [SerializeField] private TopHudView _topHudView;

    [Header("Shop UI")]
    [SerializeField] private ShopView _shopView; // <-- Добавить это поле
    [SerializeField] private List<UpgradeConfig> _upgradeConfigs;
    [SerializeField] private Button _openShopButton;

    private ShopModel _shopModel; // Добавили переменную магазина
    private ShopPresenter _shopPresenter;

    private GamePause _gamePause;
    private CursorHider _cursorHider;

    // Системы
    private ObjectPool<CarPart> _partPool;
    private FractureCalculator _fractureCalculator;
    private FractureSystem _fractureSystem;
    private DecaySystem _decaySystem;

    // Данные и логика
    private Wallet _wallet;
    private TopHudPresenter _topHudPresenter;
    private StageModel _stageModel;

    private int _activeBigParts = 0;
    private BagRandomizer _bagRandomizer;
    private Dictionary<CarPart, ObjectPool<CarPart>> _pools = new Dictionary<CarPart, ObjectPool<CarPart>>();

    private void Awake()
    {
        if (_gameConfig == null) Debug.LogError("Не назначен GameConfig в Bootstrapper!");

        // 1. Инициализация систем разрушения
        _fractureCalculator = new FractureCalculator();
        _partPool = new ObjectPool<CarPart>(_carPartPrefab, _poolContainer, initialCapacity: _gameConfig.PoolCapacity);
        _fractureSystem = new FractureSystem(GetPartFromPool, _fractureCalculator, _gameConfig);
        _decaySystem = new DecaySystem();

        _gamePause = new GamePause();
        _cursorHider = new CursorHider();

        // На старте прячем курсор (вызов читается как песня!)
        _cursorHider.Hide();

        // На старте жестко закрываем магазин
        _shopView.Close();

        if (_gameConfig.PartPrefabs != null && _gameConfig.PartPrefabs.Length > 0)
        {
            _bagRandomizer = new BagRandomizer(_gameConfig.PartPrefabs.Length);
        }

        // 2. Инициализация Экономики и Прогрессии
        _wallet = new Wallet(initialBalance: YG2.saves.balance);

        // Создаем модель магазина
        _shopModel = new ShopModel(_wallet, _upgradeConfigs);

        // Загружаем текущую стадию из YG2 (если игра запущена впервые и там 0, берем 1)
        int savedStage = YG2.saves.level < 1 ? 1 : YG2.saves.level;
        _stageModel = new StageModel(initialStage: savedStage);

        if (_openShopButton != null)
        {
            _openShopButton.onClick.AddListener(_shopView.Open);
        }

        // 3. Инициализация UI (Верхний HUD и Магазин)
        _topHudPresenter = new TopHudPresenter(_wallet, _topHudView);

        // Создаем презентер магазина, связывая UI с логикой
        _shopPresenter = new ShopPresenter(_shopModel, _shopView, _wallet, _upgradeConfigs, _stageModel);

        _shopModel.OnUpgradeChanged += HandleUpgradePurchased;

        // Связываем Модель Стадии с UI Презентером
        _stageModel.OnProgressChanged += _topHudPresenter.UpdateMachineProgress;

        // Подписываемся на прохождение уровня (чтобы спавнить новую машину)
        _stageModel.OnStageCompleted += HandleStageCompleted;

        _shopView.OnWindowOpened += _gamePause.Enable;
        _shopView.OnWindowOpened += _cursorHider.Show;

        _shopView.OnWindowClosed += _gamePause.Disable;
        _shopView.OnWindowClosed += _cursorHider.Hide;

        // 4. Настройка оружия (Загружаем сохраненный урон из магазина)
        var damageData = _shopModel.GetUpgradeData("hammer_damage");
        if (damageData != null)
        {
            _hammerCaster.SetDamage((float)damageData.value);
        }
    }

    private void Start()
    {
        // Принудительно обновляем UI на старте
        _stageModel.ForceUpdateUI();

        // Запуск спавна первой машины
        SpawnNewStagePart(_stageModel.CurrentStage);
    }

    private void Update()
    {
        _decaySystem?.Tick();

        // Спавним новую машину, как только кончились крупные детали. 
        // Не ждем, пока игрок добьет мелочь!
        if (_activeBigParts <= 0)
        {
            SpawnNewStagePart(_stageModel.CurrentStage);
        }
    }

    public CarPart GetPartFromPool(CarPart prefab)
    {
        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new ObjectPool<CarPart>(prefab, _poolContainer, initialCapacity: 20);
        }

        CarPart part = _pools[prefab].Get();
        part.SourcePrefab = prefab;
        return part;
    }

    private int GetTotalActiveParts()
    {
        int total = 0;
        foreach (var pool in _pools.Values)
        {
            total += pool.ActiveCount;
        }
        return total;
    }

    private void HandleUpgradePurchased(string upgradeId)
    {
        // Если игрок прокачал урон, обновляем его у кувалды прямо во время игры
        if (upgradeId == "hammer_damage")
        {
            var damageData = _shopModel.GetUpgradeData("hammer_damage");
            if (damageData != null)
            {
                _hammerCaster.SetDamage((float)damageData.value);
            }
        }
    }

    private void HandleStageCompleted(int newStageLevel)
    {
        // 1. Сохраняем достигнутый уровень в облако
        YG2.saves.level = newStageLevel;
        YG2.SaveProgress();

        // 2. Спавним детали для новой машины
        SpawnNewStagePart(newStageLevel);
    }

    private void SpawnNewStagePart(int stageLevel)
    {
        for (int i = 0; i < _gameConfig.StartPartsPerWave; i++)
        {
            int randomPrefabIndex = _bagRandomizer != null ? _bagRandomizer.GetNext() : 0;
            CarPart prefabToSpawn = _gameConfig.PartPrefabs[randomPrefabIndex];

            CarPart initialPart = GetPartFromPool(prefabToSpawn);

            float randomX = UnityEngine.Random.Range(-_gameConfig.SpawnRadius, _gameConfig.SpawnRadius);
            float randomZ = UnityEngine.Random.Range(-_gameConfig.SpawnRadius, _gameConfig.SpawnRadius);
            initialPart.transform.position = new Vector3(randomX, 10f, randomZ);
            initialPart.transform.localScale = Vector3.one * _gameConfig.InitialPartScale;

            float hp = _fractureCalculator.CalculateHP(generation: 0, stageLevel);
            initialPart.Setup(generation: 0, maxHp: hp);
            initialPart.OnDestroyed += HandlePartDestroyed;

            // Регистрируем появление крупной детали
            _activeBigParts++;
        }
    }

    private void HandlePartDestroyed(CarPart destroyedPart)
    {
        destroyedPart.OnDestroyed -= HandlePartDestroyed;

        // Если уничтожена крупная деталь, вычитаем её из счетчика
        if (destroyedPart.Generation < _gameConfig.MaxGenerations)
        {
            _activeBigParts--;
        }

        // Пытаемся раздробить и узнаем, сколько вылетело новых кусков
        int newFragmentsCount = _fractureSystem.ProcessFracture(destroyedPart, _stageModel.CurrentStage, HandlePartDestroyed);

        // Если вылетевшие куски всё еще крупные (не финальная мелочь) — добавляем их в счетчик
        int nextGeneration = destroyedPart.Generation + 1;
        if (nextGeneration < _gameConfig.MaxGenerations && newFragmentsCount > 0)
        {
            _activeBigParts += newFragmentsCount;
        }

        // Даем деньги, если деталь была финальной (самой мелкой) ИЛИ просто не захотела делиться
        if (destroyedPart.Generation >= _gameConfig.MaxGenerations || newFragmentsCount == 0)
        {
            _wallet.AddMoney(_gameConfig.BasePartReward);
            _stageModel.AddProgress(1);
        }
    }

    private void OnDestroy()
    {
        if (_stageModel != null)
        {
            _stageModel.OnProgressChanged -= _topHudPresenter.UpdateMachineProgress;
            _stageModel.OnStageCompleted -= HandleStageCompleted;
        }

        _topHudPresenter?.Dispose();
        _shopPresenter?.Dispose();

        if (_shopModel != null) _shopModel.OnUpgradeChanged -= HandleUpgradePurchased;

        if (_openShopButton != null) _openShopButton.onClick.RemoveListener(_shopView.Open);

        if (_shopView != null)
        {
            _shopView.OnWindowOpened -= _gamePause.Enable;
            _shopView.OnWindowOpened -= _cursorHider.Show;

            _shopView.OnWindowClosed -= _gamePause.Disable;
            _shopView.OnWindowClosed -= _cursorHider.Hide;
        }
    }
}