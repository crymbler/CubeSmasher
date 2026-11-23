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
/// ����� �����. �������� ��������� �������� � ��������� ��������������
/// � ����� �������. ���� ������ ���� � ���������������:
/// ���������, ��������, ������, UI.
/// </summary>
[DefaultExecutionOrder(-1000)] // ��������� �������� ������ ���� ������ ������ ����
public class Bootstrapper : MonoBehaviour
{
    [Header("���������")]
    [Tooltip("�������� ������ ���� �� �������� �� ������������ �������")]
    [SerializeField] private GameSettings _gameSettings;
    [SerializeField] private HammerCaster _hammerCaster;

    [Header("��� � �������")]
    [SerializeField] private CarPart _carPartPrefab;
    [SerializeField] private Transform _poolContainer;

    [Header("UI Views")]
    [SerializeField] private TopHudView _topHudView;

    [Header("Shop UI")]
    [SerializeField] private ShopView _shopView;
    [SerializeField] private List<UpgradeConfig> _upgradeConfigs;
    [SerializeField] private Button _openShopButton;

    [Header("����")]
    [Tooltip("������ Master.mixer. ����� � ��������� �������� �������� � AudioSource")]
    [SerializeField] private UnityEngine.Audio.AudioMixer _audioMixer;
    [Tooltip("������ � �����. ����� �������� ������")]
    [SerializeField] private AudioClip _musicClip;
    [Tooltip("���� ����� �� ��������� ��� ������ ��� ������ ����� (ButtonClickSound)")]
    [SerializeField] private AudioClip _clickClip;
    [Tooltip("���� ����� ������")]
    [SerializeField] private AudioClip _hammerHitClip;
    [Tooltip("���� �������� ������. ����� � ���� ������������� �����")]
    [SerializeField] private AudioClip _unlockClip;
    [Tooltip("���� ������� ������ �� �������")]
    [SerializeField] private AudioClip _splitClip;

    [Header("UI - ���������")]
    [Tooltip("����� ��������: ��������� � ����. ����� � ������ �������� �� ��������")]
    [SerializeField] private CubeSmasher.Settings.SettingsView _settingsView;
    [SerializeField] private Button _openSettingsButton;

    [Header("UI - �����")]
    [Tooltip("����� ���������: ������ � �������. ����� � ������ ������ �� ��������")]
    [SerializeField] private CubeSmasher.UI.Skins.CosmeticShopView _cosmeticShopView;
    [SerializeField] private Button _openSkinsButton;
    [SerializeField] private CubeSmasher.UI.LocalizedUiLabel[] _navLabels;

    [Header("UI - �����")]
    [SerializeField] private GarageView _garageView;
    [SerializeField] private Button _openGarageButton;

    private ServiceLocator _services;
    private PartLifecycleService _partLifecycle;
    private OfflineEarningsService _offlineEarnings;
    [SerializeField] private OfflineRewardPopup _offlineRewardPopup;
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
            return;
        }

        // ������ ������ ������ null: ������� � ��������� �� ���� �����������
        if (_upgradeConfigs == null)
        {
            _upgradeConfigs = new List<UpgradeConfig>();
        }

        // ���������� � ����� ������ ������, ��������� �� ���� ���������
        var saveService = new YandexSaveService();
        _services.Register<ISaveService>(saveService);

        // ��� �������� UI ������� ����, ����� OnDestroy ���� �� �����
        _subscriptions = new Subscriptions();
        _services.Register(_subscriptions);

        // ������� ������������� �����: ���������, ��������, ������, UI
        var initializers = new List<IGameInitializer>
        {
            new EconomyInitializer(_gameSettings, saveService, _upgradeConfigs),
            new GameplayInitializer(_gameSettings, _carPartPrefab, _poolContainer),
            new WeaponInitializer(_hammerCaster, _gameSettings.Hammer),
            new AudioInitializer(_audioMixer, _musicClip, _clickClip, _hammerHitClip, _unlockClip, _splitClip),
            new UIWiringInitializer(_topHudView, _shopView, _garageView,
                                    _openShopButton, _openGarageButton, _upgradeConfigs,
                                    _cosmeticShopView, _openSkinsButton,
                                    _settingsView, _openSettingsButton, _navLabels)
        };

        foreach (IGameInitializer initializer in initializers)
        {
            initializer.Initialize();
        }

        // ������ ��� ��������� ���������� � �������
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

        // ����� �� ����� ����������
        if (_offlineEarnings != null)
        {
            _offlineRewardPopup?.Show(_offlineEarnings.GetPendingReward());
        }

        // ������ ����� �������
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
        // ������� �� ������� YG2: ��� ����������� � ���������� ����� �����,
        // ����� ����� ����������� ������ ������� �� ����� �������
        if (_services != null && _services.TryGet(out CosmeticService cosmetics))
        {
            cosmetics.Dispose();
        }

        // ������� ������� �������� UI (� �������� �������), ����� ���������� � ���������
        _subscriptions?.Dispose();

        // ��������� ������ �� �������� �����: ������ ��� �� ����������, ���� �� ��� ���������������
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
