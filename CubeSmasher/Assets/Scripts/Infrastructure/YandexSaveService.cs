using System;
using CubeSmasher.Core.Interfaces;
using UnityEngine;
using YG;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Реализация ISaveService для Яндекс Игр (YG2).
    ///
    /// Плагин отправляет облако асинхронно и не сообщает об ошибке через исключение,
    /// поэтому успех сохранения здесь не проверяется. Поэтому каждое сохранение пишет
    /// локальную копию (LocalSaveService), а в облако уходит через YG2.SaveProgress().
    ///
    /// Ручной шаг (см. docs/MANUAL_TODO.md): подтвердить в плагине, как узнать об успехе
    /// облачного сохранения. Пока флаг hasPendingLocalSave не сбрасывается автоматически.
    /// </summary>
    public class YandexSaveService : ISaveService
    {
        public void Save()
        {
            // Локальная копия всегда, даже если облако не ответит
            LocalSaveService.Write();

            // Плагин ещё не инициализирован (например, сохранение из конструктора сервиса
            // на Awake). YG2.SaveProgress() в этом состоянии пишет LogError и выходит,
            // поэтому облако пропускаем: данные уже в PlayerPrefs.
            if (!YG2.isSDKEnabled)
            {
                YG2.saves.hasPendingLocalSave = true;
                return;
            }

            try
            {
                YG2.SaveProgress();
            }
            catch (Exception e)
            {
                // Флаг ставим, чтобы при следующем старте понимать, что облако могло не получить данные
                YG2.saves.hasPendingLocalSave = true;
                Debug.LogWarning($"[Save] Облачное сохранение не выполнено, данные сохранены локально: {e.Message}");
            }
        }

        public T Load<T>() where T : class
        {
            // YG2.saves является глобальным синглтоном SavesYG
            return YG2.saves as T;
        }

        public bool HasKey(string key)
        {
            // YG2 не предоставляет прямого API для проверки ключей
            return YG2.saves != null;
        }
    }
}
