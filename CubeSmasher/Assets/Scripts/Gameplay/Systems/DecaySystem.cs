using System.Collections.Generic;
using UnityEngine;

public class DecaySystem
{
    // Структура для хранения данных о времени жизни кубика
    private struct DecayData
    {
        public CarPart Part;
        public float ExpirationTime;

        public DecayData(CarPart part, float expirationTime)
        {
            Part = part;
            ExpirationTime = expirationTime;
        }
    }

    private readonly List<DecayData> _activeParts = new List<DecayData>(150);
    private readonly float _lifetime = 5.0f;

    // Регистрация кубика в системе
    public void Register(CarPart part)
    {
        if (part == null) return;

        // По GDD очищаем только поколение 2
        if (part.Generation < 2) return;

        _activeParts.Add(new DecayData(part, Time.time + _lifetime));
    }

    // Вызывается каждый кадр из Bootstrapper.Update()
    public void Tick()
    {
        if (_activeParts.Count == 0) return;

        float currentTime = Time.time;

        // Идем с конца списка, чтобы безопасно удалять элементы
        for (int i = _activeParts.Count - 1; i >= 0; i--)
        {
            if (currentTime >= _activeParts[i].ExpirationTime)
            {
                CarPart partToReturn = _activeParts[i].Part;

                // Проверяем, активен ли еще объект (может игрок его уже разбил)
                if (partToReturn != null && partToReturn.gameObject.activeInHierarchy)
                {
                    partToReturn.ReturnToPool();
                }

                _activeParts.RemoveAt(i);
            }
        }
    }
}