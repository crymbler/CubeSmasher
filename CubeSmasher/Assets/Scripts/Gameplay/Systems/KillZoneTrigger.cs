using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Если упавший объект поддерживает возврат в пул - возвращаем
        if (other.TryGetComponent(out IPoolable poolableObject))
        {
            poolableObject.ReturnToPool();
        }
    }
}