using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Пул для ОДНОГО префаба детали. Один компонент = один префаб, как в старом проекте:
/// у каждой из разновидностей деталей свой пул, а всё вместе лежит в словаре
/// (см. PartLifecycleService._pools).
///
/// Компонент можно повесить в сцену руками и назначить _prefab в инспекторе.
/// Если пул не назначен, PartLifecycleService создаст его сам под контейнером пула.
/// </summary>
public class CarPartPool : MonoBehaviour
{
    [Tooltip("Префаб детали, для которого работает этот пул")]
    [SerializeField] private CarPart _prefab;

    [Tooltip("Сколько объектов создать заранее при старте. 0 = создавать по требованию")]
    [SerializeField] private int _prewarm = 0;

    private Queue<CarPart> _pool;

    public CarPart Prefab => _prefab;

    /// <summary>Сколько объектов сейчас выдано наружу.</summary>
    public int ActiveCount { get; private set; }

    /// <summary>Сколько объектов лежит в пуле готовыми.</summary>
    public int FreeCount => _pool != null ? _pool.Count : 0;

    /// <summary>Настроить пул из кода (когда компонент создаётся на лету).</summary>
    public void Configure(CarPart prefab, int prewarm)
    {
        _prefab = prefab;
        _prewarm = prewarm;
        EnsureQueue();
    }

    private void Awake()
    {
        EnsureQueue();
    }

    private void EnsureQueue()
    {
        if (_pool == null)
        {
            _pool = new Queue<CarPart>();
        }
    }

    /// <summary>Создать заранее _prewarm объектов.</summary>
    public void Prewarm()
    {
        EnsureQueue();

        for (int i = 0; i < _prewarm; i++)
        {
            CarPart instance = CreateObject();
            if (instance != null)
            {
                _pool.Enqueue(instance);
            }
        }
    }

    /// <summary>
    /// Выдать деталь из пула. Если пул пуст, создаётся новый объект.
    /// Возвращает false, только если префаб не назначен.
    /// </summary>
    public bool TryGetObject(out CarPart part)
    {
        EnsureQueue();

        part = _pool.Count > 0 ? _pool.Dequeue() : CreateObject();

        if (part == null)
        {
            return false;
        }

        part.ResetState();
        part.gameObject.SetActive(true);
        ActiveCount++;
        return true;
    }

    /// <summary>Вернуть деталь в пул. Повторный возврат игнорируется.</summary>
    public void ReturnObject(CarPart part)
    {
        if (part == null) return;

        // Защита от двойного возврата: объект уже в пуле
        if (!part.gameObject.activeSelf) return;

        EnsureQueue();

        part.gameObject.SetActive(false);
        part.transform.SetParent(transform, false);
        _pool.Enqueue(part);

        if (ActiveCount > 0)
        {
            ActiveCount--;
        }
    }

    // CarPart.Initialize ожидает Action<IPoolable>
    private void ReturnFromPoolable(IPoolable poolable)
    {
        if (poolable is CarPart part)
        {
            ReturnObject(part);
        }
    }

    private CarPart CreateObject()
    {
        if (_prefab == null)
        {
            Debug.LogError("[CarPartPool] Не назначен _prefab — пул не может создавать объекты");
            return null;
        }

        CarPart instance = Instantiate(_prefab, transform);
        instance.Initialize(ReturnFromPoolable);
        instance.gameObject.SetActive(false);
        return instance;
    }
}
