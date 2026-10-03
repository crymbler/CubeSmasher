using System;
using System.Collections;
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

    [Header("UI - Гараж")]
    [SerializeField] private GarageView _garageView;
    [SerializeField] private Button _openGarageButton;

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

    private PassiveIncome _passiveIncome;

    private int _currentRootParts = 0;
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

        if (_gameConfig.PartPrefabs != null && _gameConfig.PartPrefabs.Length > 0)
        {
            _bagRandomizer = new BagRandomizer(_gameConfig.PartPrefabs.Length);
        }

        // 2. Инициализация Экономики и Прогрессии
        _wallet = new Wallet(initialBalance: YG2.saves.balance);

        // Загружаем текущую стадию из YG2 (если игра запущена впервые и там 0, берем 1)
        int savedStage = YG2.saves.level < 1 ? 1 : YG2.saves.level;
        _stageModel = new StageModel(initialStage: savedStage);

        _passiveIncome = new PassiveIncome(_gameConfig);

        _passiveIncome.OnIncomeGenerated += _wallet.AddMoney;
        _stageModel.OnStageCompleted += _passiveIncome.RecalculateIncome;

        // Создаем модель магазина
        _shopModel = new ShopModel(_wallet, _upgradeConfigs);

        if (_openShopButton != null)
        {
            _openShopButton.onClick.AddListener(_shopView.Open);
        }

        // Подписываем кнопку на главном экране на открытие гаража
        if (_openGarageButton != null)
        {
            _openGarageButton.onClick.AddListener(_garageView.Open);
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

        _shopView.OnOpened += _gamePause.Enable;
        _shopView.OnOpened += _cursorHider.Show;

        _shopView.OnClosed += _gamePause.Disable;
        _shopView.OnClosed += _cursorHider.Hide;

        _garageView.OnOpened += _gamePause.Enable;
        _garageView.OnOpened += _cursorHider.Show;

        _garageView.OnClosed += _gamePause.Disable;
        _garageView.OnClosed += _cursorHider.Hide;

        // 4. Настройка оружия (Загружаем сохраненный урон из магазина)
        var damageData = _shopModel.GetUpgradeData("hammer_damage");
        if (damageData != null)
        {
            _hammerCaster.SetDamage((float)damageData.value);
        }
    }

    private void Start()
    {
        _stageModel.ForceUpdateUI();

        if (YG2.saves.lastSaveTime > 0) // Если игрок заходит не в первый раз
        {
            // Берем мировое время по Гринвичу в секундах
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long offlineSeconds = currentTime - YG2.saves.lastSaveTime;

            // Защита от "читеров" и сломанной экономики (максимум копим за 24 часа = 86400 сек)
            long maxOfflineSeconds = 86400;
            if (offlineSeconds > maxOfflineSeconds)
            {
                offlineSeconds = maxOfflineSeconds;
            }

            if (offlineSeconds > 0 && _passiveIncome.CurrentIncomePerSecond > 0)
            {
                double earnedOffline = offlineSeconds * _passiveIncome.CurrentIncomePerSecond;
                _wallet.AddMoney(earnedOffline);

                // Выводим в консоль для проверки (позже прикрутим UI окошко)
                UnityEngine.Debug.Log($"[Оффлайн] Игрок отсутствовал {offlineSeconds} сек. Заработано: {earnedOffline} монет!");
            }
        }

        for (int i = 0; i < _gameConfig.StartPartsPerWave; i++)
        {
            SpawnRootPart();
        }

        // ЗАПУСКАЕМ АВТОСОХРАНЕНИЕ
        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
        _decaySystem?.Tick();
        _passiveIncome?.Tick(UnityEngine.Time.deltaTime);
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
    }

    private void SpawnRootPart()
    {
        int randomPrefabIndex = _bagRandomizer != null ? _bagRandomizer.GetNext() : 0;
        CarPart prefabToSpawn = _gameConfig.PartPrefabs[randomPrefabIndex];

        CarPart initialPart = GetPartFromPool(prefabToSpawn);

        float randomX = UnityEngine.Random.Range(-_gameConfig.SpawnRadius, _gameConfig.SpawnRadius);
        float randomZ = UnityEngine.Random.Range(-_gameConfig.SpawnRadius, _gameConfig.SpawnRadius);
        initialPart.transform.position = new Vector3(randomX, 10f, randomZ);
        initialPart.transform.localScale = Vector3.one * _gameConfig.InitialPartScale;

        // Берем актуальную стадию для расчета ХП
        float hp = _fractureCalculator.CalculateHP(generation: 0, _stageModel.CurrentStage);
        initialPart.Setup(generation: 0, maxHp: hp);
        initialPart.OnDestroyed += HandlePartDestroyed;

        _currentRootParts++; // Деталь появилась на столе: +1
    }

    private void HandlePartDestroyed(CarPart destroyedPart)
    {
        destroyedPart.OnDestroyed -= HandlePartDestroyed;

        // ТВОЯ ЛОГИКА: Если разбита именно ГЛАВНАЯ деталь (Gen 0)
        if (destroyedPart.Generation == 0)
        {
            _currentRootParts--; // Деталь ушла: -1
            StartCoroutine(SpawnNewPartWithDelay(2f)); // Ждем 2 секунды и спавним
        }

        // Пытаемся раздробить деталь
        int newFragmentsCount = _fractureSystem.ProcessFracture(destroyedPart, _stageModel.CurrentStage, HandlePartDestroyed);

        // Начисляем деньги и прогресс за самую мелкую деталь
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
            _shopView.OnOpened -= _gamePause.Enable;
            _shopView.OnOpened -= _cursorHider.Show;

            _shopView.OnClosed -= _gamePause.Disable;
            _shopView.OnClosed -= _cursorHider.Hide;
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);

            // Записываем точное время сохранения (в секундах)
            YG2.saves.lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            YG2.SaveProgress();
        }
    }

    private IEnumerator SpawnNewPartWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Защита: спавним новую, только если на столе их меньше лимита
        if (_currentRootParts < _gameConfig.StartPartsPerWave)
        {
            SpawnRootPart();
        }
    }
}