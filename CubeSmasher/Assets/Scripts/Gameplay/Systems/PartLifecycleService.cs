using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using UnityEngine;

/// <summary>
/// Жизненный цикл деталей: спавн корневых кубов, награда и прогресс за каждую
/// разбитую деталь, раскол на осколки, учёт деталей, покинувших стол (упали за
/// край и вернулись в пул), гниение осколков и отложенный респавн.
/// Раньше эта логика была размазана по Bootstrapper.
///
/// Ключевое отличие от старого кода: количество деталей на столе считается по
/// списку живых объектов, а не по счётчику, который уменьшался только при
/// разбитии молотом. Из-за этого раньше детали, упавшие за край, не заменялись.
/// </summary>
public class PartLifecycleService
{
    /// <summary>Задержка перед появлением нового корневого куба после потери предыдущего.</summary>
    private const float RespawnDelay = 2f;

    /// <summary>Сколько объектов заранее создаётся в пуле для нового префаба.</summary>
    private const int PoolPrewarmCapacity = 20;

    /// <summary>
    /// Гниение мелких осколков: детали поколения 2 исчезают через 5 секунд.
    /// Система DecaySystem есть в проекте, но Register в старом коде не вызывался
    /// ни разу, поэтому она ничего не делала. Включать её в коммите про рефакторинг
    /// нельзя: осколки начнут пропадать до того, как игрок их разобьёт, и доход упадёт.
    /// Поставьте true, если хотите вернуть задуманное по GDD поведение.
    /// </summary>
    private const bool EnableShardDecay = false;

    private readonly GameConfig _config;
    private readonly CarPart _carPartPrefab;
    private readonly Transform _poolContainer;
    private readonly FractureCalculator _fractureCalculator;
    private readonly DecaySystem _decaySystem;
    private readonly IWallet _wallet;
    private readonly StageModel _stageModel;
    private readonly FractureSystem _fractureSystem;

    // Делегат создаём один раз: осколок, появившийся при расколе, уходит в систему гниения
    private readonly Action<CarPart> _onFragmentSpawned;

    // Пул для каждого префаба детали
    private readonly Dictionary<CarPart, ObjectPool<CarPart>> _pools = new Dictionary<CarPart, ObjectPool<CarPart>>();

    // Корневые кубы, которые сейчас лежат на столе
    private readonly List<CarPart> _aliveRoots = new List<CarPart>();

    // Моменты времени, когда нужно заспавнить новый корневой куб
    private readonly List<float> _pendingRespawns = new List<float>();

    private BagRandomizer _bagRandomizer;

    public PartLifecycleService(GameConfig config, CarPart carPartPrefab, Transform poolContainer,
                                FractureCalculator fractureCalculator, DecaySystem decaySystem,
                                IWallet wallet, StageModel stageModel)
    {
        _config = config;
        _carPartPrefab = carPartPrefab;
        _poolContainer = poolContainer;
        _fractureCalculator = fractureCalculator;
        _decaySystem = decaySystem;
        _wallet = wallet;
        _stageModel = stageModel;

        _fractureSystem = new FractureSystem(GetPartFromPool, _fractureCalculator, _config);
        _onFragmentSpawned = (EnableShardDecay && _decaySystem != null)
            ? new Action<CarPart>(_decaySystem.Register)
            : null;
    }

    /// <summary>Первичный спавн кубов на столе.</summary>
    public void Begin()
    {
        if (_config.PartPrefabs != null && _config.PartPrefabs.Length > 0)
        {
            _bagRandomizer = new BagRandomizer(_config.PartPrefabs.Length);
        }

        // Прогреваем пул основного префаба, чтобы первый спавн не тормозил
        if (_carPartPrefab != null)
        {
            GetPool(_carPartPrefab, _config.PoolCapacity);
        }

        for (int i = 0; i < _config.StartPartsPerWave; i++)
        {
            SpawnRootPart();
        }
    }

    public void Tick(float deltaTime)
    {
        TickPendingRespawns();
        _decaySystem?.Tick();
    }

    /// <summary>Взять деталь из пула (используется системой раскола).</summary>
    public CarPart GetPartFromPool(CarPart prefab)
    {
        CarPart part = GetPool(prefab, PoolPrewarmCapacity).Get();
        part.SourcePrefab = prefab;
        return part;
    }

    private ObjectPool<CarPart> GetPool(CarPart prefab, int initialCapacity)
    {
        if (!_pools.TryGetValue(prefab, out ObjectPool<CarPart> pool))
        {
            pool = new ObjectPool<CarPart>(prefab, _poolContainer, initialCapacity);
            _pools[prefab] = pool;
        }

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
        if (_config.PartPrefabs == null || _config.PartPrefabs.Length == 0) return;

        int prefabIndex = _bagRandomizer != null ? _bagRandomizer.GetNext() : 0;
        CarPart prefab = _config.PartPrefabs[prefabIndex];

        CarPart part = GetPartFromPool(prefab);

        float randomX = UnityEngine.Random.Range(-_config.SpawnRadius, _config.SpawnRadius);
        float randomZ = UnityEngine.Random.Range(-_config.SpawnRadius, _config.SpawnRadius);
        part.transform.position = new Vector3(randomX, 10f, randomZ);
        part.transform.localScale = Vector3.one * _config.InitialPartScale;

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
        double reward = _config.BasePartReward
                        * Mathf.Pow(_config.PartRewardMultiplier, destroyedPart.Generation);

        _wallet.Add(reward);
        _stageModel.AddProgress(1);

        // Новые осколки отдаём системе гниения: она сама решает, какие поколения чистить
        _fractureSystem.ProcessFracture(
            destroyedPart, _stageModel.CurrentStage, HandlePartDestroyed, _onFragmentSpawned);

        if (wasRootPart)
        {
            ScheduleRespawn();
        }
    }

    /// <summary>Деталь ушла со стола не разбитой (упала за край) — учитываем и планируем замену.</summary>
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
        if (planned < _config.StartPartsPerWave)
        {
            _pendingRespawns.Add(Time.time + RespawnDelay);
        }
    }
}
