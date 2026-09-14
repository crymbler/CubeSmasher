 using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(ColorChanger))]
public class CarPart : MonoBehaviour, IDestructible, IPoolable
{
    private Action<IPoolable> _returnToPool;
    private Rigidbody _rigidbody;
    private ColorChanger _colorChanger;
    private float _currentHp;
    private int _generation;
    private MeshFilter _meshFilter;
    private BoxCollider _boxCollider;

    // Событие смерти. На него подпишется система раскола и экономика
    public event Action<CarPart> OnDestroyed;

    // Чтобы осколки знали, какой префаб копировать при взрыве
    public CarPart SourcePrefab { get; set; }

    public int Generation => _generation;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _boxCollider = GetComponent<BoxCollider>();
        _rigidbody = GetComponent<Rigidbody>();
        _colorChanger = GetComponent<ColorChanger>();
    }

    // --- Реализация IPoolable ---
    public void Initialize(Action<IPoolable> returnAction)
    {
        _returnToPool = returnAction;
    }

    public void ResetState()
    {
        _currentHp = 0f;
        _generation = 0;
        transform.localScale = Vector3.one;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        _colorChanger.ApplyAcidColor();
    }

    public Mesh CurrentMesh => _meshFilter.mesh;

    public void ReturnToPool()
    {
        _returnToPool?.Invoke(this);
    }

    // --- Игровая логика ---
    public void Setup(int generation, float maxHp)
    {
        _generation = generation;
        _currentHp = maxHp;
    }

    // --- Реализация IDestructible ---
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        if (_currentHp <= 0f) return;

        _currentHp -= amount;

        // TODO: Партиклы искр (Play Particle [Hit_Sparks])
        // TODO: Звук удара (Play Sound [ASMR_Hit_Pitch_Randomized])

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO: Звук раскола (Play Sound [ASMR_Crunch])
        OnDestroyed?.Invoke(this);
        ReturnToPool();
    }
}