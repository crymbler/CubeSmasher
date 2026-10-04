using System;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Абстракция для сервиса сохранения/загрузки данных.
    /// Изолирует зависимость от конкретной платформы (YG2, PlayerPrefs, etc.)
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Сохранить текущие данные
        /// </summary>
        void Save();

        /// <summary>
        /// Загрузить данные указанного типа
        /// </summary>
        T Load<T>() where T : class;

        /// <summary>
        /// Проверить существование ключа
        /// </summary>
        bool HasKey(string key);
    }
}
