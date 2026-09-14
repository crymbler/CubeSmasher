using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class FractureSystem
{
    private readonly Func<CarPart, CarPart> _getPartMethod;
    private readonly FractureCalculator _calculator;
    private readonly GameConfig _config; // <-- Добавили конфиг

    private float _explosionForce = 300f;
    private float _explosionRadius = 2f;

    // Конструктор теперь принимает GameConfig
    public FractureSystem(Func<CarPart, CarPart> getPartMethod, FractureCalculator calculator, GameConfig config)
    {
        _getPartMethod = getPartMethod;
        _calculator = calculator;
        _config = config;
    }

    // БЫЛО: public void ProcessFracture(...)
    // СТАЛО:
    public int ProcessFracture(CarPart destroyedPart, int currentStage, Action<CarPart> onPartDestroyedCallback)
    {
        if (!_calculator.TrySplit(destroyedPart.Generation, _config.BaseSplitChance))
            return 0; // Возвращаем 0, если деталь не разделилась

        int fragmentsCount = Random.Range(2, 5);
        int nextGeneration = destroyedPart.Generation + 1;
        float newHp = _calculator.CalculateHP(nextGeneration, currentStage);

        for (int i = 0; i < fragmentsCount; i++)
        {
            CarPart fragment = _getPartMethod(destroyedPart.SourcePrefab);
            fragment.transform.localScale = destroyedPart.transform.localScale * _config.SplitScaleMultiplier;

            Vector3 randomOffset = Random.insideUnitSphere * 0.2f;
            fragment.transform.position = destroyedPart.transform.position + randomOffset;

            fragment.Setup(nextGeneration, newHp);
            fragment.OnDestroyed += onPartDestroyedCallback;

            if (fragment.TryGetComponent(out Rigidbody rb))
            {
                rb.AddExplosionForce(_explosionForce, destroyedPart.transform.position, _explosionRadius);
            }
        }

        return fragmentsCount; // Возвращаем количество созданных осколков
    }
}