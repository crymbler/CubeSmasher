using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Молот: объединяет наведение и анимацию (HammerCast) с цифрами урона (HammerDamageText).
/// Считает урон одного удара с учётом крита и бьёт всё в радиусе.
/// Значения статов приходят снаружи (WeaponInitializer берёт их из StatService).
/// </summary>
public class HammerCaster : MonoBehaviour
{
    [Header("Настройки луча и удара")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _hitRadius = 1.5f;
    [SerializeField] private float _maxDistance = 100f;
    [SerializeField] private LayerMask _hitMask;

    [Header("Иерархия кувалды")]
    [SerializeField] private Transform _cursorTarget;
    [SerializeField] private Transform _swingPivot;
    [SerializeField] private float _followSpeed = 25f;

    [Header("Анимация")]
    [SerializeField] private float _idleAngle = -20f;
    [SerializeField] private float _windupAngle = -60f;
    [SerializeField] private float _hitAngle = 90f;
    [SerializeField] private float _windupTime = 0.1f;
    [SerializeField] private float _swingTime = 0.1f;
    [SerializeField] private float _returnTime = 0.25f;

    [Header("Эффекты")]
    [SerializeField] private DamagePopup _damagePopupPrefab;
    [SerializeField] private Color _normalDamageColor = Color.white;
    [SerializeField] private Color _critDamageColor = Color.red;
    [SerializeField] private Color _sparkColor = new Color(1f, 0.75f, 0.2f);
    [SerializeField] private int _sparkCount = 18;
    [SerializeField] private float _shockwaveForce = 4f;

    private HammerCast _cast;
    private HammerDamageText _damageText;
    private ParticleSystem _sparks;

    private float _damage = 10f;
    private float _critChance;
    private float _critDamage = 2f;

    private void Start()
    {
        if (_camera == null) _camera = Camera.main;

        _cast = new HammerCast(this, _camera, _cursorTarget, _swingPivot, _hitMask,
                               _maxDistance, _followSpeed,
                               _idleAngle, _windupAngle, _hitAngle,
                               _windupTime, _swingTime, _returnTime);

        _damageText = new HammerDamageText(_damagePopupPrefab)
        {
            NormalColor = _normalDamageColor,
            CritColor = _critDamageColor
        };

        _sparks = CreateSparks();
        _cast.Hit += HandleHit;
        _swingPivot.localRotation = Quaternion.Euler(_idleAngle, 0, 0);

        Cursor.visible = false;
    }

    private void Update()
    {
        _cast.Move();

        if (Input.GetMouseButtonDown(0) && !_cast.IsSwinging)
            _cast.Swing();
    }

    /// <summary>Урон, радиус, скорость, крит — из StatService через WeaponInitializer.</summary>
    public void SetCombatStats(float damage, float radius, float attackSpeed,
                               float critChance, float critDamage)
    {
        _damage = damage;

        if (radius > 0f) _hitRadius = Mathf.Clamp(radius, MinHitRadius, MaxHitRadius);
        if (_cast != null) _cast.SpeedMultiplier = Mathf.Max(0.1f, attackSpeed);

        _critChance = Mathf.Clamp01(critChance);
        _critDamage = Mathf.Max(1f, critDamage);
    }

    /// <summary>Цвета цифр урона — из HammerConfig.</summary>
    public void SetDamageColors(Color normal, Color crit)
    {
        _normalDamageColor = normal;
        _critDamageColor = crit;

        if (_damageText == null) return;
        _damageText.NormalColor = normal;
        _damageText.CritColor = crit;
    }

    private const float MinHitRadius = 0.5f;
    private const float MaxHitRadius = 6f;

    private void HandleHit(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, _hitRadius, _hitMask);
        if (hits.Length > 0)
        {
            PlayHitSound();
            _sparks.transform.position = position;
            _sparks.Emit(_sparkCount);
        }

        var pushed = new HashSet<Rigidbody>();
        foreach (Collider hit in hits)
        {
            if (hit.attachedRigidbody == null || !pushed.Add(hit.attachedRigidbody)) continue;
            hit.attachedRigidbody.AddExplosionForce(_shockwaveForce, position, _hitRadius, 0f, ForceMode.Impulse);
        }

        // Крит бросается один раз на взмах, а не на каждую деталь: иначе при попадании
        // по трём кубам один был бы критом, а другой нет — выглядит как баг
        bool isCrit = Random.value < _critChance;
        float damage = isCrit ? _damage * _critDamage : _damage;

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(out IDestructible destructible)) continue;

            destructible.TakeDamage(damage);
            DevTrace.Hit(isCrit);
            if (_damageText != null) _damageText.Show(hit.transform.position, damage, isCrit);
        }
    }

    /// <summary>
    /// Звук удара играет один раз за взмах, даже если молот попал по нескольким деталям сразу.
    /// </summary>
    private ParticleSystem CreateSparks()
    {
        var go = new GameObject("Sparks");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = _sparkColor;
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = UnlockEffect.MakeMaterial(_sparkColor);
        return ps;
    }

    private void PlayHitSound()
    {
        if (!CubeSmasher.Infrastructure.ServiceLocator.Instance.TryGet(
                out CubeSmasher.Infrastructure.AudioService audio)) return;

        audio.PlaySfxVaried(CubeSmasher.UI.AudioClipLibrary.HammerHit);
    }
}
