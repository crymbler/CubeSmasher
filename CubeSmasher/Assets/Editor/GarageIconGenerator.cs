using System.IO;
using CubeSmasher.Data.Configs;
using CubeSmasher.UI.Skins;
using UnityEditor;
using UnityEngine;

public static class GarageIconGenerator
{
    private const string IconFolder = "Assets/Art/GarageIcons";
    private const float PixelsPerUnit = 100f;

    [MenuItem("CubeSmasher/Garage/Generate Car Icons")]
    public static void Generate()
    {
        string[] guids = AssetDatabase.FindAssets("t:GarageConfig");
        if (guids.Length != 1)
        {
            Debug.LogError($"[Гараж] Нужен ровно один GarageConfig, найдено: {guids.Length}");
            return;
        }

        var config = AssetDatabase.LoadAssetAtPath<GarageConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

        if (!AssetDatabase.IsValidFolder(IconFolder))
        {
            AssetDatabase.CreateFolder("Assets/Art", "GarageIcons");
        }

        var serialized = new SerializedObject(config);
        SerializedProperty cars = serialized.FindProperty("GarageCars");

        int created = 0;
        for (int i = 0; i < config.GarageCars.Length; i++)
        {
            GarageCarData car = config.GarageCars[i];
            string name = string.IsNullOrEmpty(car.Key) ? $"car{i:00}" : car.Key;

            Sprite sprite = BakeIcon(car.ModelPrefab, $"{IconFolder}/{name}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[Гараж] Машина {i} ({name}): иконка не создана, проверьте ModelPrefab", config);
                continue;
            }

            cars.GetArrayElementAtIndex(i).FindPropertyRelative("MiniIcon").objectReferenceValue = sprite;
            created++;
        }

        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Гараж] Иконок создано: {created} из {config.GarageCars.Length}");
    }

    private static Sprite BakeIcon(GameObject model, string assetPath)
    {
        if (model == null) return null;

        Texture2D texture = ThumbnailBaker.Render(model, null);
        if (texture == null) return null;

        File.WriteAllBytes(Path.Combine(ProjectRoot(), assetPath), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static string ProjectRoot() => Path.GetDirectoryName(Application.dataPath);
}
