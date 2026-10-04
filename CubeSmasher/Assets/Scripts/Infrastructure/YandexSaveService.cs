using CubeSmasher.Core.Interfaces;
using YG;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Реализация ISaveService для платформы Yandex Games (YG2).
    /// Изолирует прямую зависимость от YG2 API.
    /// </summary>
    public class YandexSaveService : ISaveService
    {
        public void Save()
        {
            YG2.SaveProgress();
        }

        public T Load<T>() where T : class
        {
            // YG2.saves является глобальным синглтоном SavesYG
            // Для типобезопасности возвращаем его как T
            return YG2.saves as T;
        }

        public bool HasKey(string key)
        {
            // YG2 не предоставляет прямой API для проверки ключей
            // В текущей архитектуре все данные хранятся в SavesYG
            // Возвращаем true, если SavesYG инициализирован
            return YG2.saves != null;
        }
    }
}
