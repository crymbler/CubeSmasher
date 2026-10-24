using UnityEngine;

/// <summary>
/// Эффект открытия машины: вспышка частиц и короткий звук.
/// Создаётся кодом, если в инспекторе GarageView ничего не назначено,
/// поэтому правка префаба не нужна. Назначенные клипы и партиклы имеют приоритет.
/// </summary>
public static class UnlockEffect
{
    private const int SampleRate = 44100;

    /// <summary>Партикл-вспышка в точке. Создаётся один раз и переиспользуется.</summary>
    public static ParticleSystem CreateBurst(Transform anchor)
    {
        var go = new GameObject("UnlockBurst");
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.useUnscaledTime = true;                                // работает при Time.timeScale = 0
        main.stopAction = ParticleSystemStopAction.Destroy;         // удаляется сам по окончании
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = CubeSmasher.UI.UITheme.NeonGreen;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Шейдер подбираем сами: если пусто, частицы выйдут розовыми
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            var mat = new Material(shader) { color = CubeSmasher.UI.UITheme.NeonGreen };
            renderer.sharedMaterial = mat;
        }

        return ps;
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
