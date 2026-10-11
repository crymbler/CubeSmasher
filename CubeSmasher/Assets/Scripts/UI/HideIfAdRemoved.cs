using UnityEngine;
using YG;

public class HideIfAdRemoved : MonoBehaviour
{
    private void Start()
    {
        // Проверяем статус при старте сцены, если данные Яндекса уже загружены
        if (YG2.isSDKEnabled)
        {
            CheckPurchaseStatus();
        }
    }

    private void OnEnable()
    {
        // Подписываемся на событие загрузки сохранений (на случай, если данные грузятся чуть дольше)
        YG2.onGetSDKData += CheckPurchaseStatus; 
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= CheckPurchaseStatus;
    }

    private void CheckPurchaseStatus()
    {
        // Если в сохранениях значится, что реклама отключена — прячем эту кнопку
        if (YG2.saves.isAdsRemoved)
        {
            gameObject.SetActive(false);
        }
    }
}