using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class FractureSystem
{
    private readonly Func<CarPart, CarPart> _getPartMethod;
    private readonly FractureCalculator _calculator;

    private float _explosionForce = 300f;
    private float _explosionRadius = 2f;

    public FractureSystem(Func<CarPart, CarPart> getPartMethod, FractureCalculator calculator)
    {
        _getPartMethod = getPartMethod;
        _calculator = calculator;
    }

    public void ProcessFracture(CarPart destroyedPart, int currentStage, Action<CarPart> onPartDestroyedCallback)
    {
        float baseSplitChance = 0.85f;
        if (!_calculator.TrySplit(destroyedPart.Generation, baseSplitChance)) return;

        int fragmentsCount = Random.Range(2, 5);
        int nextGeneration = destroyedPart.Generation + 1;
        float newHp = _calculator.CalculateHP(nextGeneration, currentStage);

        for (int i = 0; i < fragmentsCount; i++)
        {
            // «апрашиваем из пула клон той же самой детали
            CarPart fragment = _getPartMethod(destroyedPart.SourcePrefab);

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