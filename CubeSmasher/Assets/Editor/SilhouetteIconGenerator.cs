using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PixelIconGenerator
{
    const int Size = 100;
    const int Grid = 20;
    const int Cell = Size / Grid;
    const string OutDir = "Assets/UI/Icons";

    static readonly (string name, Func<float, float, bool> shape)[] Icons =
    {
        ("icon_speed_attack", (u, v) => Poly(u, v, 0.66f,0.98f, 0.18f,0.50f, 0.46f,0.50f, 0.34f,0.02f, 0.82f,0.52f, 0.54f,0.52f)),
        ("icon_parts", (u, v) => Box(u, v, 0.08f,0.08f,0.45f,0.45f) || Box(u, v, 0.55f,0.08f,0.92f,0.45f) || Box(u, v, 0.08f,0.55f,0.45f,0.92f) || Box(u, v, 0.55f,0.55f,0.92f,0.92f)),
        ("icon_damage", (u, v) => Seg(u, v, 0.35f,0.35f, 0.88f,0.88f, 0.12f) || Seg(u, v, 0.16f,0.44f, 0.44f,0.16f, 0.10f) || Seg(u, v, 0.30f,0.30f, 0.10f,0.10f, 0.10f)),
        ("icon_radius", (u, v) => Frame(u, v, 0.05f,0.05f,0.95f,0.95f, 0.12f) || Box(u, v, 0.38f,0.38f,0.62f,0.62f)),
        ("icon_anim_speed", (u, v) => Frame(u, v, 0.08f,0.08f,0.92f,0.92f, 0.12f) || Seg(u, v, 0.5f,0.5f, 0.5f,0.75f, 0.10f) || Seg(u, v, 0.5f,0.5f, 0.72f,0.5f, 0.10f)),
        ("icon_crit_chance", Cut(Burst, Percent)),
        ("icon_crit_damage", Cut(Burst, Arrow)),
        ("icon_garage", (u, v) => Poly(u, v, 0.5f,0.95f, 0.05f,0.55f, 0.95f,0.55f) || (Box(u, v, 0.15f,0.05f,0.85f,0.55f) && !Box(u, v, 0.38f,0.0f,0.62f,0.30f))),
        ("icon_upgrade_shop", (u, v) => Box(u, v, 0.22f,0.34f,0.80f,0.78f) && !Box(u, v, 0.44f,0.0f,0.48f,1f) || Box(u, v, 0.0f,0.0f,0.0f,0.0f) || Seg(u, v, 0.02f,0.94f, 0.16f,0.94f, 0.07f) || Seg(u, v, 0.16f,0.94f, 0.22f,0.78f, 0.07f) || Circle(u, v, 0.36f,0.14f, 0.07f) || Circle(u, v, 0.80f,0.14f, 0.07f)),
        ("icon_skin_shop", (u, v) => Poly(u, v, 0.05f,0.62f, 0.27f,0.88f, 0.38f,0.80f, 0.50f,0.90f, 0.62f,0.80f, 0.73f,0.88f, 0.95f,0.62f, 0.78f,0.45f, 0.78f,0.10f, 0.22f,0.10f, 0.22f,0.45f)),
        ("icon_settings", (u, v) => (Box(u, v, 0.25f,0.25f,0.75f,0.75f) || Box(u, v, 0.42f,0.05f,0.58f,0.95f) || Box(u, v, 0.05f,0.42f,0.95f,0.58f) || Seg(u, v, 0.22f,0.22f, 0.78f,0.78f, 0.16f) || Seg(u, v, 0.22f,0.78f, 0.78f,0.22f, 0.16f)) && !Circle(u, v, 0.5f,0.5f, 0.13f)),
    };

    [MenuItem("Tools/Generate Pixel Icons")]
    static void Generate()
    {
        Directory.CreateDirectory(OutDir);

        foreach (var (name, shape) in Icons)
        {
            var tex = Render(shape);
            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        AssetDatabase.Refresh();

        foreach (var (name, _) in Icons)
        {
            string path = $"{OutDir}/{name}.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log($"Generated {Icons.Length} pixel icons in {OutDir}");
    }

    static Texture2D Render(Func<float, float, bool> shape)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var pixels = new Color32[Size * Size];

        for (int y = 0; y < Size; y++)

            for (int x = 0; x < Size; x++)
            {
                float u = (x / Cell + 0.5f) / Grid;
                float v = (y / Cell + 0.5f) / Grid;
                pixels[y * Size + x] = shape(u, v) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static Func<float, float, bool> Cut(Func<float, float, bool> background, Func<float, float, bool> foreground)
        => (u, v) => foreground(u, v) || (background(u, v) && !Near(foreground, u, v));

    static bool Near(Func<float, float, bool> f, float u, float v)
    {
        const float r = 0.07f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            if (f(u + Mathf.Cos(a) * r, v + Mathf.Sin(a) * r)) return true;
        }
        return f(u, v);
    }

    static bool Burst(float u, float v)
    {
        float dx = u - 0.52f, dy = v - 0.5f;
        float k = Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * Mathf.Atan2(dy, dx))), 3f);
        return Mathf.Sqrt(dx * dx + dy * dy) <= 0.22f + 0.24f * k;
    }

    static bool Arrow(float u, float v)
        => Poly(u, v, 0.42f, 0.92f, 0.12f, 0.56f, 0.30f, 0.56f, 0.30f, 0.02f, 0.54f, 0.02f, 0.54f, 0.56f, 0.72f, 0.56f);

    static bool Percent(float u, float v)
        => Circle(u, v, 0.28f, 0.74f, 0.10f) || Circle(u, v, 0.72f, 0.26f, 0.10f) || Seg(u, v, 0.18f, 0.84f, 0.82f, 0.16f, 0.12f);

    static bool Box(float u, float v, float x0, float y0, float x1, float y1)
        => u >= x0 && u <= x1 && v >= y0 && v <= y1;

    static bool Frame(float u, float v, float x0, float y0, float x1, float y1, float t)
        => Box(u, v, x0, y0, x1, y1) && !Box(u, v, x0 + t, y0 + t, x1 - t, y1 - t);

    static bool Circle(float u, float v, float cx, float cy, float r)
        => (u - cx) * (u - cx) + (v - cy) * (v - cy) <= r * r;

    static bool Seg(float u, float v, float ax, float ay, float bx, float by, float thick)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((u - ax) * dx + (v - ay) * dy) / (dx * dx + dy * dy));
        float px = ax + dx * t - u, py = ay + dy * t - v;
        return px * px + py * py <= (thick * 0.5f) * (thick * 0.5f);
    }

    static bool Poly(float u, float v, params float[] p)
    {
        bool inside = false;
        int n = p.Length / 2;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = p[i * 2], yi = p[i * 2 + 1];
            float xj = p[j * 2], yj = p[j * 2 + 1];
            if ((yi > v) != (yj > v) && u < (xj - xi) * (v - yi) / (yj - yi) + xi)
                inside = !inside;
        }
        return inside;
    }
}