using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using UnityEngine;

/// <summary>
/// Жизненный цикл деталей: спавн корневых кубов, награда и прогресс за каждую
/// разбитую деталь, раскол на осколки, учёт деталей, покинувших стол, и респавн.
///
/// Пулы: у каждого префаба детали свой CarPartPool (как в старом проекте, где на
/// каждую из разновидностей был отдельный словарь). Все пулы лежат в _pools.
/// Пул создаётся по требованию под _poolContainer, поэтому сцену настраивать не нужно.
/// </summary>
public class PartLifecycleService
{
    /// <summary>Задержка перед появлением нового корневого куба после потери предыдущего.</summary>
    private float RespawnDelay => _physics.RespawnDelay > 0f ? _physics.RespawnDelay : 2f;

    /// <summary>Сколько объектов создаётся заранее в новом пуле. Держим малым ради быстрой загрузки.</summary>
    private const int PoolWarmup = 4;

    /// <summary>
    /// Гниение мелких осколков: детали поколения 2 исчезают через 5 секунд.
    /// Включать только вместе с переработкой награды, иначе игрок теряет доход.
    /// </summary>
    private const bool EnableShardDecay = false;

    private readonly PhysicsConfig _physics;
    private readonly EconomyConfig _economy;
    private readonly CarPart _carPartPrefab;
    private readonly Transform _poolContainer;
    private readonly FractureCalculator _fractureCalculator;
    private readonly DecaySystem _decaySystem;
    private readonly IWallet _wallet;
    private readonly StageModel _stageModel;
    private readonly FractureSystem _fractureSystem;
    private readonly Action<CarPart> _onFragmentSpawned;

    /// <summary>Пул на каждый префаб детали: ключ — префаб, значение — его пул.</summary>
    private readonly Dictionary<CarPart, CarPartPool> _pools = new Dictionary<CarPart, CarPartPool>();

    /// <summary>Корневые кубы, которые сейчас лежат на столе.</summary>
    private readonly List<CarPart> _aliveRoots = new List<CarPart>();

    /// <summary>Моменты времени, когда нужно заспавнить новый корневой куб.</summary>
    private readonly List<float> _pendingRespawns = new List<float>();

    private BagRandomizer _bagRandomizer;

    public PartLifecycleService(PhysicsConfig physics, EconomyConfig economy, CarPart carPartPrefab,
                                Transform poolContainer,
                                FractureCalculator fractureCalculator, DecaySystem decaySystem,
                                IWallet wallet, StageModel stageModel)
    {
        _physics = physics;
        _economy = economy;
        _carPartPrefab = carPartPrefab;
        _poolContainer = poolContainer;
        _fractureCalculator = fractureCalculator;
        _decaySystem = decaySystem;
        _wallet = wallet;
        _stageModel = stageModel;

        _fractureSystem = new FractureSystem(GetPartFromPool, _fractureCalculator, _physics);
        _onFragmentSpawned = (EnableShardDecay && _decaySystem != null)
            ? new Action<CarPart>(_decaySystem.Register)
            : null;
    }

    /// <summary>Первичный спавн кубов на столе.</summary>
    public void Begin()
    {
        if (_physics.PartPrefabs != null && _physics.PartPrefabs.Length > 0)
        {
            _bagRandomizer = new BagRandomizer(_physics.PartPrefabs.Length);

            // Пулы создаём для всех префабов сразу, чтобы не подтормаживало в бою
            for (int i = 0; i < _physics.PartPrefabs.Length; i++)
            {
                GetPool(_physics.PartPrefabs[i]).Prewarm();
            }
        }

        for (int i = 0; i < _physics.StartPartsPerWave; i++)
        {
            SpawnRootPart();
        }
    }

    public void Tick(float deltaTime)
    {
        TickPendingRespawns();
        _decaySystem?.Tick();
    }

    /// <summary>Взять деталь из пула её префаба (используется системой раскола).</summary>
    public CarPart GetPartFromPool(CarPart prefab)
    {
        CarPartPool pool = GetPool(prefab);

        if (!pool.TryGetObject(out CarPart part))
        {
            return null;
        }

        part.SourcePrefab = prefab;
        return part;
    }

    /// <summary>Пул для префаба. Создаётся под _poolContainer при первом обращении.</summary>
    private CarPartPool GetPool(CarPart prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[PartLifecycleService] Передан null вместо префаба детали");
            return null;
        }

        if (_pools.TryGetValue(prefab, out CarPartPool pool))
        {
            return pool;
        }

        var holder = new GameObject($"Pool_{prefab.name}");
        holder.transform.SetParent(_poolContainer, false);

        pool = holder.AddComponent<CarPartPool>();
        pool.Configure(prefab, PoolWarmup);

        _pools[prefab] = pool;
        return pool;
    }

    private void TickPendingRespawns()
    {
        if (_pendingRespawns.Count == 0) return;

        float now = Time.time;

        for (int i = _pendingRespawns.Count - 1; i >= 0; i--)
        {
            if (now < _pendingRespawns[i]) continue;

            _pendingRespawns.RemoveAt(i);
            SpawnRootPart();
        }
    }

    private void SpawnRootPart()
    {
        if (_physics.PartPrefabs == null || _physics.PartPrefabs.Length == 0) return;

        int prefabIndex = _bagRandomizer != null ? _bagRandomizer.GetNext() : 0;
        CarPart prefab = _physics.PartPrefabs[prefabIndex];

        CarPart part = GetPartFromPool(prefab);
        if (part == null) return;

        float randomX = UnityEngine.Random.Range(-_physics.SpawnRadius, _physics.SpawnRadius);
        float randomZ = UnityEngine.Random.Range(-_physics.SpawnRadius, _physics.SpawnRadius);
        part.transform.position = new Vector3(randomX, 10f, randomZ);
        part.transform.localScale = Vector3.one * _physics.InitialPartScale;

        float hp = _fractureCalculator.CalculateHP(generation: 0, _stageModel.CurrentStage);
        part.Setup(generation: 0, maxHp: hp);

        part.OnDestroyed += HandlePartDestroyed;
        part.OnReturnedToPool += HandleRootReturnedToPool;

        _aliveRoots.Add(part);
    }

    /// <summary>Деталь разбита молотом: награда, прогресс и попытка раскола.</summary>
    private void HandlePartDestroyed(CarPart destroyedPart)
    {
        destroyedPart.OnDestroyed -= HandlePartDestroyed;
        destroyedPart.OnReturnedToPool -= HandleRootReturnedToPool;

        bool wasRootPart = _aliveRoots.Remove(destroyedPart);

        // Награда за каждую разбитую деталь; чем глубже поколение, тем дороже осколок
        double reward = _economy.BasePartReward
                        * Mathf.Pow(_economy.PartRewardMultiplier, destroyedPart.Generation);

        _wallet.Add(reward);
        _stageModel.AddProgress(1);

        // Новые осколки уходят в систему гниения (если она включена)
        _fractureSystem.ProcessFracture(
            destroyedPart, _stageModel.CurrentStage, HandlePartDestroyed, _onFragmentSpawned);

        if (wasRootPart)
        {
            ScheduleRespawn();
        }
    }

    /// <summary>Деталь ушла со стола не разбитой (упала за край) — планируем замену.</summary>
    private void HandleRootReturnedToPool(CarPart part)
    {
        if (!_aliveRoots.Remove(part)) return;

        part.OnDestroyed -= HandlePartDestroyed;
        part.OnReturnedToPool -= HandleRootReturnedToPool;

        ScheduleRespawn();
    }

    private void ScheduleRespawn()
    {
        int planned = _aliveRoots.Count + _pendingRespawns.Count;
        if (planned < _physics.StartPartsPerWave)
        {
            _pendingRespawns.Add(Time.time + RespawnDelay);
        }
    }
}
