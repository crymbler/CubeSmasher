using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Каст молота: наведение за мышью и анимация замаха-удара-возврата.
/// Про урон и про то, кого бить, не знает.
/// </summary>
public class HammerCast
{
    private readonly MonoBehaviour _host;
    private readonly Camera _camera;
    private readonly Transform _cursorTarget;
    private readonly Transform _swingPivot;
    private readonly LayerMask _hitMask;

    private readonly float _maxDistance;
    private readonly float _followSpeed;
    private readonly float _idleAngle;
    private readonly float _windupAngle;
    private readonly float _hitAngle;
    private readonly float _windupTime;
    private readonly float _swingTime;
    private readonly float _returnTime;

    /// <summary>Удар нанесён: момент, когда молот внизу. Аргумент — точка удара.</summary>
    public event Action<Vector3> Hit;

    /// <summary>Скорость удара: 1 — обычная, 2 — вдвое быстрее. Делит длительности фаз.</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    public bool IsSwinging { get; private set; }

    public Vector3 Position => _cursorTarget != null ? _cursorTarget.position : Vector3.zero;

    public HammerCast(MonoBehaviour host, Camera camera, Transform cursorTarget, Transform swingPivot,
                      LayerMask hitMask, float maxDistance, float followSpeed,
                      float idleAngle, float windupAngle, float hitAngle,
                      float windupTime, float swingTime, float returnTime)
    {
        _host = host;
        _camera = camera;
        _cursorTarget = cursorTarget;
        _swingPivot = swingPivot;
        _hitMask = hitMask;
        _maxDistance = maxDistance;
        _followSpeed = followSpeed;
        _idleAngle = idleAngle;
        _windupAngle = windupAngle;
        _hitAngle = hitAngle;
        _windupTime = windupTime;
        _swingTime = swingTime;
        _returnTime = returnTime;
    }

    public void Move()
    {
        if (IsSwinging || _cursorTarget == null) return;

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition = Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hitMask)
            ? hit.point
            : ray.GetPoint(10f);

        _cursorTarget.position = Vector3.Lerp(_cursorTarget.position, targetPosition, Time.deltaTime * _followSpeed);
        _cursorTarget.rotation = Quaternion.identity;
    }

    public void Swing()
    {
        if (IsSwinging) return;
        _host.StartCoroutine(Routine());
    }

    private float Phase(float duration) => duration / Mathf.Max(0.01f, SpeedMultiplier);

    private IEnumerator Routine()
    {
        IsSwinging = true;

        Quaternion idleRotation = Quaternion.Euler(_idleAngle, 0, 0);
        Quaternion windupRotation = Quaternion.Euler(_windupAngle, 0, 0);
        Quaternion hitRotation = Quaternion.Euler(_hitAngle, 0, 0);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / Phase(_windupTime);
            _swingPivot.localRotation = Quaternion.Lerp(idleRotation, windupRotation, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / Phase(_swingTime);
            _swingPivot.localRotation = Quaternion.Lerp(windupRotation, hitRotation, t);
            yield return null;
        }

        Hit?.Invoke(Position);

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / Phase(_returnTime);
            _swingPivot.localRotation = Quaternion.Lerp(hitRotation, idleRotation, t);
            yield return null;
        }

        _swingPivot.localRotation = idleRotation;
        IsSwinging = false;
    }
}
