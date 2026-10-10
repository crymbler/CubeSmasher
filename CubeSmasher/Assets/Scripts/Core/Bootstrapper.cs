using System;
using System.Collections;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using CubeSmasher.Infrastructure;
using CubeSmasher.Infrastructure.Initializers;
using UnityEngine;
using UnityEngine.UI;
using YG;

/// <summary>
/// Точка входа. Собирает контейнер сервисов и запускает инициализаторы
/// в явном порядке. Сама логика живёт в инициализаторах:
/// экономика, геймплей, оружие, UI.
/// </summary>
[DefaultExecutionOrder(-1000)] // контейнер сервисов должен быть собран раньше всех
public class Bootstrapper : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Корневой конфиг игры со ссылками на тематические конфиги")]
    [SerializeField] private GameSettings _gameSettings;
    [SerializeField] private HammerCaster _hammerCaster;

    [Header("Пул и Префабы")]
    [SerializeField] private CarPart _carPartPrefab;
    [SerializeField] private Transform _poolContainer;

    [Header("UI Views")]
    [SerializeField] private TopHudView _topHudView;

    [Header("Shop UI")]
    [SerializeField] private ShopView _shopView;
    [SerializeField] private List<UpgradeConfig> _upgradeConfigs;
    [SerializeField] private Button _openShopButton;

    [Header("UI - Гараж")]
    [SerializeField] private GarageView _garageView;
    [SerializeField] private Button _openGarageButton;

    private ServiceLocator _services;
    private PartLifecycleService _partLifecycle;
    private OfflineEarningsService _offlineEarnings;
    private IPassiveIncomeService _passiveIncome;
    private TopHudPresenter _topHudPresenter;
    private ShopPresenter _shopPresenter;
    private Subscriptions _subscriptions;

    private void Awake()
    {
        _services = ServiceLocator.Instance;
        _services.Clear();

        if (_gameSettings == null)
        {
            Debug.LogError("Не назначен GameSettings в Bootstrapper!");
            return;
        }

        // Пустой список вместо null: магазин и презентер по нему итерируются
        if (_upgradeConfigs == null)
        {
            Debug.LogWarning("Список UpgradeConfig пуст в Bootstrapper, магазин будет без апгрейдов");
            _upgradeConfigs = new List<UpgradeConfig>();
        }

        // Сохранение — самый первый сервис, остальные на него опираются
        var saveService = new YandexSaveService();
        _services.Register<ISaveService>(saveService);

        // Все подписки UI пишутся сюда, чтобы OnDestroy снял их разом
        _subscriptions = new Subscriptions();
        _services.Register(_subscriptions);

        // Порядок инициализации явный: экономика, геймплей, оружие, UI
        var initializers = new List<IGameInitializer>
        {
            new EconomyInitializer(_gameSettings, saveService, _upgradeConfigs),
            new GameplayInitializer(_gameSettings, _carPartPrefab, _poolContainer),
            new WeaponInitializer(_hammerCaster),
            new UIWiringInitializer(_topHudView, _shopView, _garageView,
                                    _openShopButton, _openGarageButton, _upgradeConfigs)
        };

        foreach (IGameInitializer initializer in initializers)
        {
            initializer.Initialize();
        }

        // Ссылки для кадрового обновления и очистки
        _services.TryGet(out _partLifecycle);
        _services.TryGet(out _offlineEarnings);
        _services.TryGet(out _passiveIncome);
        _services.TryGet(out _topHudPresenter);
        _services.TryGet(out _shopPresenter);
    }

    private void Start()
    {
        if (_services.TryGet(out StageModel stageModel))
        {
            stageModel.ForceUpdateUI();
        }

        // Доход за время отсутствия
        _offlineEarnings?.Apply();

        // Первая волна деталей
        _partLifecycle?.Begin();

        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _partLifecycle?.Tick(dt);
        _passiveIncome?.Tick(dt);
    }

    private void OnDestroy()
    {
        // Сначала снимаем подписки UI (в обратном порядке), затем презентеры и контейнер
        _subscriptions?.Dispose();

        // Презентер гаража не хранится полем: достаём его из контейнера, пока он ещё зарегистрирован
        if (_services != null && _services.TryGet(out GaragePresenter garagePresenter))
        {
            garagePresenter.Dispose();
        }

        _topHudPresenter?.Dispose();
        _shopPresenter?.Dispose();

        _services?.Clear();
    }

    private IEnumerator AutoSaveRoutine()
    {
        var wait = new WaitForSeconds(10f);

        while (true)
        {
            yield return wait;

            YG2.saves.lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (_services.TryGet(out ISaveService saveService))
            {
                saveService.Save();
            }
        }
    }
}
