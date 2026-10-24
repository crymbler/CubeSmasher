using UnityEngine;

/// <summary>
/// Эффект открытия машины: два залпа частиц (зелёный и розовый неон) и короткий звук.
/// Настройки частиц здесь, ими пользуются и рантайм, и сборщик префабов
/// (Tools > CubeSmasher > Create Unlock Effect Assets), поэтому они не расходятся.
/// </summary>
public static class UnlockEffect
{
    public static readonly Color NeonGreen = new Color(0.612f, 1f, 0.239f);    // #9CFF3D
    public static readonly Color NeonPink = new Color(0.957f, 0.318f, 0.965f);  // #F451F6

    /// <summary>Частиц в одном залпе. Два залпа дают 1000 всего.</summary>
    public const int BurstCount = 500;

    private const int SampleRate = 44100;

    public static void ConfigureBurst(ParticleSystem ps, Color color)
    {
        var main = ps.main;
        main.useUnscaledTime = true;                        // работает при Time.timeScale = 0 (открыт гараж)
        main.stopAction = ParticleSystemStopAction.Destroy; // экземпляр удаляется сам
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSize = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
        main.startColor = color;
        main.gravityModifier = 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = BurstCount + 50;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, BurstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2.5f;
        shape.position = new Vector3(0f, 1.5f, 0f); // над кузовом
    }

    /// <summary>Неактивный шаблон в сцене, если поля в инспекторе пустые.</summary>
    public static ParticleSystem CreateTemplate(Transform anchor, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ConfigureBurst(ps, color);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = MakeMaterial(color);
        go.SetActive(false);
        return ps;
    }

    public static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return mat;
    }

    /// <summary>Короткий восходящий «щелчок»: синус, частота растёт, громкость затухает.</summary>
    public static AudioClip CreateUnlockClip()
    {
        const float duration = 0.45f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var data = new float[samples];

        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float progress = t / duration;
            float freq = Mathf.Lerp(440f, 1320f, progress);
            phase += 2f * Mathf.PI * freq / SampleRate;
            float envelope = Mathf.Pow(1f - progress, 2f) * 0.35f;
            data[i] = Mathf.Sin(phase) * envelope;
        }

        var clip = AudioClip.Create("UnlockChime", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
