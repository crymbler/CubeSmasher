using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class FractureSystem
{
    private readonly ObjectPool<CarPart> _pool;
    private readonly FractureCalculator _calculator;

    private readonly float _explosionForce = 400f;
    private readonly float _explosionRadius = 2.5f;

    public FractureSystem(ObjectPool<CarPart> pool, FractureCalculator calculator)
    {
        if (pool == null) Debug.LogError("[FractureSystem] Pool is null!");
        if (calculator == null) Debug.LogError("[FractureSystem] Calculator is null!");

        _pool = pool;
        _calculator = calculator;
    }

    // Добавили int currentStage в параметры
    public void ProcessFracture(CarPart destroyedPart, int currentStage, Action<CarPart> onPartDestroyedCallback)
    {
        if (destroyedPart == null) return;
        if (onPartDestroyedCallback == null) return;

        // Увеличенный лимит для создания хаоса
        if (_pool.ActiveCount >= 250) return;

        // Рассчитываем шанс деления
        float baseSplitChance = 0.85f;
        if (!_calculator.TrySplit(destroyedPart.Generation, baseSplitChance)) return;

        int fragmentsCount = Random.Range(2, 5);

        // ИСПРАВЛЕНИЕ ТУТ: Передаем новое поколение и текущую стадию
        int nextGeneration = destroyedPart.Generation + 1;
        float newHp = _calculator.CalculateHP(nextGeneration, currentStage);

        for (int i = 0; i < fragmentsCount; i++)
        {
            CarPart fragment = _pool.Get();

            fragment.transform.localScale = destroyedPart.transform.localScale * 0.5f;
            Vector3 randomOffset = Random.insideUnitSphere * 0.2f;
            fragment.transform.position = destroyedPart.transform.position + randomOffset;

            fragment.Setup(nextGeneration, newHp);
            fragment.OnDestroyed += onPartDestroyedCallback;

            if (fragment.TryGetComponent(out Rigidbody rb))
            {
                rb.AddExplosionForce(_explosionForce, destroyedPart.transform.position, _explosionRadius);
            }
        }
    }
}