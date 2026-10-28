using System.Collections;
using UnityEngine;

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
    [SerializeField] private float _windupAngle = -60f; // Угол замаха (отклонение назад)
    [SerializeField] private float _hitAngle = 90f;
    [SerializeField] private float _windupTime = 0.1f; // Время на замах (очень быстро)
    [SerializeField] private float _swingTime = 0.1f;  // Время на сам удар
    [SerializeField] private float _returnTime = 0.25f;

    [Header("Эффекты")]
    [SerializeField] private DamagePopup _damagePopupPrefab;

    private float _currentDamage = 10f;
    private bool _isSwinging = false;

    private void Start()
    {
        if (_camera == null) _camera = Camera.main;
        _swingPivot.localRotation = Quaternion.Euler(_idleAngle, 0, 0);

        // Скрываем курсор, чтобы не мешался (останется только молот)
        Cursor.visible = false;
    }

    private void Update()
    {
        MoveHammer();

        if (Input.GetMouseButtonDown(0) && !_isSwinging)
        {
            StartCoroutine(SwingRoutine());
        }
    }

    public void SetDamage(float damage)
    {
        _currentDamage = damage;
    }

    /// <summary>
    /// Радиус удара. Приходит из апгрейда hammer_radius.
    /// Ограничен: в ассете апгрейда BaseValue = 10, без рамки молот бил бы по всему столу.
    /// </summary>
    public void SetHitRadius(float radius)
    {
        if (radius <= 0f) return;
        _hitRadius = Mathf.Clamp(radius, MinHitRadius, MaxHitRadius);
    }

    private const float MinHitRadius = 0.5f;
    private const float MaxHitRadius = 6f;

    private void MoveHammer()
    {
        if (_isSwinging) return;

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition;

        if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hitMask))
        {
            targetPosition = hit.point;
        }
        else
        {
            targetPosition = ray.GetPoint(10f);
        }

        // Перемещаем цель за мышкой
        _cursorTarget.position = Vector3.Lerp(_cursorTarget.position, targetPosition, Time.deltaTime * _followSpeed);

        // Фиксируем ротацию, чтобы молот всегда смотрел ровно, не подстраиваясь под камеру
        _cursorTarget.rotation = Quaternion.identity;
    }

    private IEnumerator SwingRoutine()
    {
        _isSwinging = true;

        Quaternion idleRotation = Quaternion.Euler(_idleAngle, 0, 0);
        Quaternion windupRotation = Quaternion.Euler(_windupAngle, 0, 0);
        Quaternion hitRotation = Quaternion.Euler(_hitAngle, 0, 0);

        // Фаза 1: Замах (Wind-up) - быстро отводим молот назад
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / _windupTime;
            _swingPivot.localRotation = Quaternion.Lerp(idleRotation, windupRotation, t);
            yield return null;
        }

        // Фаза 2: Удар (Strike) - резко опускаем вниз
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / _swingTime;
            _swingPivot.localRotation = Quaternion.Lerp(windupRotation, hitRotation, t);
            yield return null;
        }

        Hit(); // Наносим урон, когда молот внизу

        // Фаза 3: Возврат (Return) - плавно возвращаемся в исходное положение
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / _returnTime;
            _swingPivot.localRotation = Quaternion.Lerp(hitRotation, idleRotation, t);
            yield return null;
        }

        _swingPivot.localRotation = idleRotation;
        _isSwinging = false;
    }

    [Header("Звук")]
    [Tooltip("Разброс питча удара: 0.08 даёт заметный, но не писклявый разброс")]
    [SerializeField, Range(0f, 0.3f)] private float _hitPitchJitter = 0.08f;

    private void Hit()
    {
        Collider[] hits = Physics.OverlapSphere(_cursorTarget.position, _hitRadius, _hitMask);
        if (hits.Length > 0) PlayHitSound();

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out IDestructible destructible))
            {
                destructible.TakeDamage(_currentDamage);

                // Спавним вылетающий урон со случайным смещением, чтобы цифры не слипались
                if (_damagePopupPrefab != null)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                    DamagePopup popup = Instantiate(_damagePopupPrefab, hit.transform.position + randomOffset, Quaternion.identity);
                    popup.Setup(_currentDamage);
                }
            }
        }
    }

    /// <summary>
    /// Звук удара играет один раз за взмах, даже если молот попал по нескольким
    /// деталям сразу: иначе при попадании по трём кубам прозвучали бы три удара
    /// одновременно, и звук превратился бы в треск.
    /// </summary>
    private void PlayHitSound()
    {
        if (!CubeSmasher.Infrastructure.ServiceLocator.Instance.TryGet(
                out CubeSmasher.Infrastructure.AudioService audio)) return;

        audio.PlaySfxWithJitter(CubeSmasher.UI.AudioClipLibrary.HammerHit, _hitPitchJitter);
    }
}