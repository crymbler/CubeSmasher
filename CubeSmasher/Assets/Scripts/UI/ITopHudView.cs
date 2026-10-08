/// <summary>
/// Контракт HUD: баланс и прогресс сборки. Презентер работает только с ним.
/// </summary>
public interface ITopHudView
{
    void UpdateBalance(double balance);
    void UpdateProgress(int currentDetails, int maxDetails, string stageName);
}
