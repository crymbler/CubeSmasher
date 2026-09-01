using UnityEngine;

public class GamePause
{
    public void Enable()
    {
        Time.timeScale = 0f;
    }

    public void Disable()
    {
        Time.timeScale = 1f;
    }
}