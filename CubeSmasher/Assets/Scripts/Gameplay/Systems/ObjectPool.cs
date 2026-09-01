using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour, IPoolable
{
    private readonly T _prefab;
    private readonly Transform _container;
    private readonly Queue<T> _pool = new Queue<T>();

    // Свойство для контроля Hard Limit (120 объектов для WebGL)
    public int ActiveCount { get; private set; }

    public ObjectPool(T prefab, Transform container, int initialCapacity = 10)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPool] Prefab is null!");
            return;
        }
        if (container == null)
        {
            Debug.LogError("[ObjectPool] Container is null!");
            return;
        }

        _prefab = prefab;
        _container = container;

        for (int i = 0; i < initialCapacity; i++)
        {
            CreateNewObject();
        }
    }

    private T CreateNewObject()
    {
        T instance = Object.Instantiate(_prefab, _container);
        instance.Initialize(ReturnObject);
        instance.gameObject.SetActive(false);
        _pool.Enqueue(instance);
        return instance;
    }

    public T Get()
    {
        T instance = _pool.Count > 0 ? _pool.Dequeue() : CreateNewObject();
        instance.ResetState();
        instance.gameObject.SetActive(true);
        ActiveCount++;
        return instance;
    }

    private void ReturnObject(IPoolable poolable)
    {
        if (poolable is T instance)
        {
            instance.gameObject.SetActive(false);
            _pool.Enqueue(instance);
            ActiveCount--;
        }
    }
}