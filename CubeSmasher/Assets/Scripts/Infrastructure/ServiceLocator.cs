using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeSmasher.Infrastructure
{
    /// <summary>
    /// Простой контейнер сервисов (Service Locator).
    /// Отвечает только за хранение и выдачу зависимостей по типу интерфейса.
    ///
    /// Регистрация выполняется один раз в Bootstrapper при старте игры,
    /// после чего любой объект может получить нужную зависимость вместо
    /// прямого создания конкретного класса или обращения к статике YG2.
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator _instance;

        /// <summary>Единственный экземпляр контейнера.</summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceLocator();
                }
                return _instance;
            }
        }

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        private ServiceLocator() { }

        /// <summary>Зарегистрировать сервис под типом T (обычно интерфейсом).</summary>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Попытка зарегистрировать null как {typeof(T).Name}");
                return;
            }

            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Сервис {type.Name} уже зарегистрирован — пропускаем");
                return;
            }

            _services[type] = service;
        }

        /// <summary>Попытаться получить сервис. Возвращает null, если он не зарегистрирован.</summary>
        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object found))
            {
                service = found as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <summary>Получить сервис. Бросает исключение, если сервис не зарегистрирован.</summary>
        public T Get<T>() where T : class
        {
            if (TryGet<T>(out T service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] Сервис {typeof(T).Name} не зарегистрирован. " +
                "Убедитесь, что он добавлен в Bootstrapper.Awake()");
        }

        /// <summary>Удалить все сервисы (вызывается при выгрузке сцены).</summary>
        public void Clear()
        {
            _services.Clear();
        }
    }
}
