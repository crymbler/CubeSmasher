using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > CubeSmasher > Create Unlock Effect Assets
/// Создаёт два префаба залпа (зелёный и розовый неон) и их материалы, назначает их
/// и клип на GarageView в открытой сцене. Можно запускать повторно.
/// </summary>
public static class UnlockAssetsBuilder
{
    private const string EffectsFolder = "Assets/Prefabs/Effects";
    private const string ArtFolder = "Assets/Art/Effects";
    private const string GreenPrefab = EffectsFolder + "/UnlockBurstGreen.prefab";
    private const string PinkPrefab = EffectsFolder + "/UnlockBurstPink.prefab";
    private const string GreenMat = ArtFolder + "/UnlockBurstGreen.mat";
    private const string PinkMat = ArtFolder + "/UnlockBurstPink.mat";
    private const string OldPrefab = EffectsFolder + "/UnlockBurst.prefab";
    private const string OldMat = ArtFolder + "/UnlockBurst.mat";
    private const string ClipPath = "Assets/Audio/UnlockChime.wav";

    [MenuItem("Tools/CubeSmasher/Create Unlock Effect Assets")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs", "Effects");
        EnsureFolder("Assets/Art", "Effects");

        AssetDatabase.DeleteAsset(OldPrefab);
        AssetDatabase.DeleteAsset(OldMat);

        GameObject green = MakeBurst("UnlockBurstGreen", GreenPrefab, GreenMat, UnlockEffect.NeonGreen);
        GameObject pink = MakeBurst("UnlockBurstPink", PinkPrefab, PinkMat, UnlockEffect.NeonPink);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null) Debug.LogWarning("[Unlock] Не найден " + ClipPath);

        int assigned = AssignToGarageView(green.GetComponent<ParticleSystem>(), pink.GetComponent<ParticleSystem>(), clip);
        Debug.Log("[Unlock] Готово: два залпа по " + UnlockEffect.BurstCount + " частиц. " +
            (assigned > 0 ? "Назначено на GarageView в сцене." : "GarageView в открытой сцене не найден."));
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static GameObject MakeBurst(string name, string prefabPath, string matPath, Color color)
    {
        AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(UnlockEffect.MakeMaterial(color), matPath);
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        UnlockEffect.ConfigureBurst(ps, color);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static int AssignToGarageView(ParticleSystem green, ParticleSystem pink, AudioClip clip)
    {
        int count = 0;
        foreach (var view in Resources.FindObjectsOfTypeAll<GarageView>())
        {
            if (view == null || !view.gameObject.scene.IsValid()) continue; // только объекты сцены
            var so = new SerializedObject(view);
            so.FindProperty("_unlockParticles").objectReferenceValue = green;
            so.FindProperty("_unlockParticlesSecondary").objectReferenceValue = pink;
            so.FindProperty("_unlockSound").objectReferenceValue = clip;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            count++;
        }
        return count;
    }
}
