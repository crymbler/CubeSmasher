namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Шаг инициализации игры. Каждый инициализатор отвечает за свою область
    /// (экономика, геймплей, оружие, UI) и получает внешние ссылки через конструктор.
    /// </summary>
    public interface IGameInitializer
    {
        void Initialize();
    }
}
