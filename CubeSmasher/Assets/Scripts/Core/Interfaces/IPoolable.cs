using System;
public interface IPoolable
{
    void Initialize(Action<IPoolable> returnAction);
    void ReturnToPool();
    void ResetState();
}