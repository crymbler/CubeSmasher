using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > CubeSmasher > Create Unlock Effect Assets
/// Создаёт префаб партикла открытия машины и материал, сохраняет их в проект
/// и назначает клип и партикл на GarageView в открытой сцене.
/// Можно запускать повторно: файлы перезаписываются.
/// </summary>
public static class UnlockAssetsBuilder
{
    private const string EffectsFolder = "Assets/Prefabs/Effects";
    private const string ArtFolder = "Assets/Art/Effects";
    private const string PrefabPath = EffectsFolder + "/UnlockBurst.prefab";
    private const string MatPath = ArtFolder + "/UnlockBurst.mat";
    private const string ClipPath = "Assets/Audio/UnlockChime.wav";

    [MenuItem("Tools/CubeSmasher/Create Unlock Effect Assets")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs", "Effects");
        EnsureFolder("Assets/Art", "Effects");

        Material mat = MakeMaterial();
        GameObject prefab = MakePrefab(mat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null) Debug.LogWarning("[Unlock] Не найден " + ClipPath + ". Проверьте, что файл лежит в проекте.");

        int assigned = AssignToGarageView(prefab.GetComponent<ParticleSystem>(), clip);

        Debug.Log("[Unlock] Готово: " + PrefabPath + (assigned > 0 ? ". Назначено на GarageView в сцене." :
            ". GarageView в открытой сцене не найден: перетащите префаб и клип в поля вручную."));
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Material MakeMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.shader = shader;
        mat.color = new Color(0.612f, 1f, 0.239f); // неоновый зелёный, как UITheme.NeonGreen
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject MakePrefab(Material mat)
    {
        var go = new GameObject("UnlockBurst");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.612f, 1f, 0.239f);
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static int AssignToGarageView(ParticleSystem ps, AudioClip clip)
    {
        int count = 0;
        foreach (var view in Resources.FindObjectsOfTypeAll<GarageView>())
        {
            if (view == null || !view.gameObject.scene.IsValid()) continue; // только объекты сцены, не ассеты
            var so = new SerializedObject(view);
            so.FindProperty("_unlockParticles").objectReferenceValue = ps;
            so.FindProperty("_unlockSound").objectReferenceValue = clip;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            count++;
        }
        return count;
    }
}
