// VoxelTools.cs  (v6 — металл выключен, мягкое свечение, диагностика и починка света)
// КУДА ПОЛОЖИТЬ: Assets/Editor/VoxelTools.cs     Меню: Tools -> Voxel
// ВАЖНО: если у тебя в проекте уже лежит старый VoxelTools.cs — УДАЛИ его,
// чтобы не было двух классов с одним именем (ошибка "duplicate class").

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VoxelTools
{
    // ================== ПАЛИТРА МОДЕЛИ (8 цветов) ==================
    static readonly string[] PaletteHexes =
    {
        "1A2438", "2E3350", "3D4470", "4A5080",
        "7B8FE0", "95A6ED",
        "9CFF3D", // салатовый (кольцо + молоты)
        "F451F6", // маджента (розовый неон)
    };

    static readonly string[] NeonHexes = { "F451F6", "9CFF3D" };

    // СИЛА СВЕЧЕНИЯ.
    // 0.5 = мягкое свечение, лаймовый остаётся зелёным.
    // Выше 0.8 зелёный канал переполняется — салатовый выглядит ЖЁЛТЫМ.
    const float EmissionStrength = 1.2f;

    // ГЛЯНЕЦ по цветам. Metallic всегда 0 — металл без отражений даёт черноту.
    // 0 = матовое, 1 = зеркальное.
    static float SmoothnessFor(string hex)
    {
        switch (hex)
        {
            case "1A2438": return 0.10f;
            case "2E3350": return 0.35f;  // пол — будет блестеть
            case "3D4470": return 0.30f;  // стены
            case "4A5080": return 0.35f;
            case "7B8FE0": return 0.50f;
            case "95A6ED": return 0.55f;
            case "9CFF3D": return 0.30f;
            case "F451F6": return 0.30f;
            default: return 0.20f;
        }
    }
    // ==============================================================


    // ---------- 0. Палитра ----------
    [MenuItem("Tools/Voxel/0. Показать цвета палитры", priority = 0)]
    public static void LogPaletteColors()
    {
        if (!TryGetSelected(out var mf, out var mr)) return;

        var tex = GetBaseTexture(mr.sharedMaterial);
        if (tex == null) { Debug.LogError("[VoxelTools] Нет текстуры-палитры."); return; }

        var counts = new Dictionary<Color32, int>();
        foreach (var c in ReadPixels(tex))
        {
            counts.TryGetValue(c, out int n);
            counts[c] = n + 1;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[VoxelTools] '{tex.name}' {tex.width}x{tex.height}, цветов: {counts.Count}");
        foreach (var kv in counts.OrderByDescending(k => k.Value))
            sb.AppendLine($"   #{ColorUtility.ToHtmlStringRGB(kv.Key)}   пикселей: {kv.Value}");

        Debug.Log(sb.ToString());
    }


    // ---------- 1. Анализ ----------
    [MenuItem("Tools/Voxel/1. Анализ модели по цветам", priority = 1)]
    public static void AnalyzeModel()
    {
        if (!TryGetSelected(out var mf, out var mr)) return;

        var mesh = mf.sharedMesh;
        var tex = GetBaseTexture(mr.sharedMaterial);
        if (mesh == null || tex == null) { Debug.LogError("[VoxelTools] Нужен меш + текстура."); return; }

        var px = ReadPixels(tex);
        var groups = GroupTris(mesh.triangles, mesh.uv, px, tex.width, tex.height, mesh.vertices);
        var neonSet = new HashSet<string>(NeonHexes.Select(x => x.ToUpper()));

        var rows = groups.OrderByDescending(kv => kv.Value.Area).Select(kv =>
        {
            var c = kv.Value.Center();
            var s = kv.Value.Size();
            return $"   #{kv.Key}{(neonSet.Contains(kv.Key) ? " <НЕОН>" : "       ")} " +
                   $"треуг.: {kv.Value.Tris.Count / 3,6}  площадь: {kv.Value.Area,8:F2}  " +
                   $"центр: ({c.x,6:F2},{c.y,6:F2},{c.z,6:F2})  размер: ({s.x,6:F2},{s.y,6:F2},{s.z,6:F2})";
        });

        Debug.Log($"[VoxelTools] '{mesh.name}': {mesh.triangles.Length / 3} треуг., {groups.Count} цветов\n" +
                  string.Join("\n", rows));
    }


    // ---------- 2. Починить текстуру ----------
    [MenuItem("Tools/Voxel/2. Починить текстуру палитры", priority = 2)]
    public static void FixPaletteTexture()
    {
        if (!TryGetSelected(out var mf, out var mr)) return;

        var tex = GetBaseTexture(mr.sharedMaterial);
        if (tex == null) { Debug.LogError("[VoxelTools] Нет текстуры-палитры."); return; }

        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (imp == null) { Debug.LogError("[VoxelTools] Нет TextureImporter."); return; }

        imp.textureType = TextureImporterType.Default;
        imp.sRGBTexture = true;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = 256;
        imp.SaveAndReimport();

        Debug.Log($"[VoxelTools] '{tex.name}': Point, без мипов, без сжатия.");
    }


    // ---------- 3. Маска свечения ----------
    [MenuItem("Tools/Voxel/3. ВАРИАНТ А: создать Emission Mask", priority = 3)]
    public static void CreateEmissionMask()
    {
        if (!TryGetSelected(out var mf, out var mr)) return;

        var tex = GetBaseTexture(mr.sharedMaterial);
        if (tex == null) { Debug.LogError("[VoxelTools] Нет текстуры-палитры."); return; }

        string texPath = AssetDatabase.GetAssetPath(tex);
        if (string.IsNullOrEmpty(texPath)) { Debug.LogError("[VoxelTools] Текстура не ассет."); return; }

        var px = ReadPixels(tex);
        var neonSet = new HashSet<string>(NeonHexes.Select(x => x.ToUpper()));

        var outPx = new Color32[px.Length];
        int hits = 0;
        for (int i = 0; i < px.Length; i++)
        {
            string hex = NearestPaletteHex(px[i]);
            if (neonSet.Contains(hex))
            {
                var c = HexToColor32(hex);
                outPx[i] = new Color32(c.r, c.g, c.b, 255);
                hits++;
            }
            else outPx[i] = new Color32(0, 0, 0, 255);
        }

        var outTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        outTex.SetPixels32(outPx);
        outTex.Apply();

        string dir = Path.GetDirectoryName(texPath).Replace('\\', '/');
        string outPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{dir}/{Path.GetFileNameWithoutExtension(texPath)}_EmissionMask.png");

        File.WriteAllBytes(outPath, outTex.EncodeToPNG());
        Object.DestroyImmediate(outTex);
        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

        var imp = AssetImporter.GetAtPath(outPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        Debug.Log($"[VoxelTools] Неоновых пикселей: {hits}. Маска: {outPath}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(outPath));
    }


    // ---------- 4. Опознание ----------
    [MenuItem("Tools/Voxel/4. Разложить по цветам (опознание)", priority = 4)]
    public static void ColorPreview()
    {
        if (!TryGetSelected(out var mf, out var mr)) return;

        var srcMesh = mf.sharedMesh;
        var tex = GetBaseTexture(mr.sharedMaterial);
        if (srcMesh == null || tex == null) { Debug.LogError("[VoxelTools] Нужен меш + текстура."); return; }

        var px = ReadPixels(tex);
        var groups = GroupTris(srcMesh.triangles, srcMesh.uv, px, tex.width, tex.height, srcMesh.vertices);

        ClearPreviewSilent();

        var root = new GameObject("ColorPreview_" + mf.gameObject.name);
        Undo.RegisterCreatedObjectUndo(root, "Color Preview");
        root.transform.position = mf.transform.position;
        root.transform.rotation = mf.transform.rotation;

        var neonSet = new HashSet<string>(NeonHexes.Select(x => x.ToUpper()));
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var bounds = srcMesh.bounds;
        float step = Mathf.Max(bounds.size.x, bounds.size.z) * 1.2f + 1f;

        int i = 0;
        foreach (var kv in groups.OrderByDescending(kv => kv.Value.Tris.Count))
        {
            var mesh = new Mesh { name = "Preview_" + kv.Key };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(srcMesh.vertices);
            if (srcMesh.normals != null && srcMesh.normals.Length == srcMesh.vertexCount)
                mesh.SetNormals(srcMesh.normals);
            mesh.SetUVs(0, srcMesh.uv);
            mesh.SetTriangles(kv.Value.Tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject($"#{kv.Key}" + (neonSet.Contains(kv.Key) ? "_NEON" : ""));
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(i * step, 0, 0);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var mat = new Material(shader) { name = "Preview_" + kv.Key };
            ApplyTo(mat, kv.Key);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;

            i++;
        }

        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log($"[VoxelTools] Разложено {i} копий. Удали через 'Tools/Voxel/5'.");
    }

    [MenuItem("Tools/Voxel/5. Удалить превью цветов", priority = 5)]
    public static void ClearPreview()
    {
        int n = ClearPreviewSilent();
        Debug.Log($"[VoxelTools] Удалено объектов превью: {n}");
    }

    static int ClearPreviewSilent()
    {
        var all = Object.FindObjectsOfType<GameObject>()
            .Where(g => g != null && g.name.StartsWith("ColorPreview_"))
            .ToArray();
        foreach (var g in all) Undo.DestroyObjectImmediate(g);
        return all.Length;
    }


    // ---------- 6. ВАРИАНТ Б: разбить по цветам ----------
    [MenuItem("Tools/Voxel/6. ВАРИАНТ Б: разбить меш по цветам", priority = 6)]
    public static void SplitMesh()
    {
        if (!TryGetSelected(out var mf, out var mr)) return;

        var srcMesh = mf.sharedMesh;
        var tex = GetBaseTexture(mr.sharedMaterial);
        if (srcMesh == null || tex == null) { Debug.LogError("[VoxelTools] Нужен меш + текстура."); return; }

        var px = ReadPixels(tex);
        var groups = GroupTris(srcMesh.triangles, srcMesh.uv, px, tex.width, tex.height, srcMesh.vertices);

        string baseFolder = $"Assets/VoxelSplit/{mf.gameObject.name}";
        Directory.CreateDirectory($"{baseFolder}/Meshes");
        Directory.CreateDirectory($"{baseFolder}/Materials");
        AssetDatabase.Refresh();

        var newMesh = new Mesh { name = srcMesh.name + "_Split" };
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.SetVertices(srcMesh.vertices);
        newMesh.SetUVs(0, srcMesh.uv);
        if (srcMesh.normals != null && srcMesh.normals.Length == srcMesh.vertexCount)
            newMesh.SetNormals(srcMesh.normals);

        var ordered = groups.OrderByDescending(kv => kv.Value.Tris.Count).ToList();
        newMesh.subMeshCount = ordered.Count;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mats = new Material[ordered.Count];

        for (int s = 0; s < ordered.Count; s++)
        {
            newMesh.SetTriangles(ordered[s].Value.Tris, s);

            var mat = new Material(shader) { name = "Mat_" + ordered[s].Key };
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            ApplyTo(mat, ordered[s].Key);

            AssetDatabase.CreateAsset(mat, $"{baseFolder}/Materials/{mat.name}.mat");
            mats[s] = mat;
        }

        if (newMesh.normals == null || newMesh.normals.Length == 0)
            newMesh.RecalculateNormals();

        AssetDatabase.CreateAsset(newMesh, $"{baseFolder}/Meshes/{newMesh.name}.asset");
        AssetDatabase.SaveAssets();

        Undo.RegisterCompleteObjectUndo(new Object[] { mf, mr }, "Split Voxel Mesh");
        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{baseFolder}/Meshes/{newMesh.name}.asset");
        mr.sharedMaterials = mats;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VoxelTools] Разбито на {mats.Length} материалов. Папка: {baseFolder}");
    }


    // ---------- 7. ПОЧИНИТЬ МАТЕРИАЛЫ (главное сейчас) ----------
    [MenuItem("Tools/Voxel/7. Починить материалы (Metallic = 0)", priority = 7)]
    public static void FixMaterialColors()
    {
        var mats = new List<Material>();

        foreach (var o in Selection.objects)
            if (o is Material m) mats.Add(m);

        if (mats.Count == 0 && Selection.activeGameObject != null)
        {
            var mr = Selection.activeGameObject.GetComponent<MeshRenderer>()
                  ?? Selection.activeGameObject.GetComponentInChildren<MeshRenderer>();
            if (mr != null) mats.AddRange(mr.sharedMaterials);
        }

        if (mats.Count == 0)
        {
            EditorUtility.DisplayDialog("Voxel Tools",
                "Выдели объект модели в Hierarchy\nили материалы в окне Project.", "Ок");
            return;
        }

        var log = new System.Text.StringBuilder();
        int done = 0;

        foreach (var mat in mats)
        {
            if (mat == null) continue;
            string hex = HexFromName(mat.name);
            if (hex == null)
            {
                log.AppendLine($"   ? {mat.name} — HEX не распознан, пропущен");
                continue;
            }

            ApplyTo(mat, hex);

            log.AppendLine($"   #{hex}   Metallic 0   Smoothness {SmoothnessFor(hex):F2}" +
                           (IsNeon(hex) ? $"   <НЕОН> свечение {EmissionStrength}" : ""));
            done++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VoxelTools] Обновлено материалов: {done}\n{log}\n" +
                  $"Свечение = {EmissionStrength}, металл выключен везде.");
    }


    // ---------- 8. Диагностика освещения ----------
    [MenuItem("Tools/Voxel/8. Диагностика освещения сцены", priority = 8)]
    public static void DiagnoseLighting()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Свет] ---- ОКРУЖЕНИЕ (Lighting > Environment) ----");
        sb.AppendLine($"   Skybox:              {(RenderSettings.skybox == null ? "ПУСТО - причина темноты!" : RenderSettings.skybox.name)}");
        sb.AppendLine($"   Ambient Mode:        {RenderSettings.ambientMode}");
        sb.AppendLine($"   Ambient Sky Color:   {RenderSettings.ambientSkyColor}");
        sb.AppendLine($"   Ambient Intensity:   {RenderSettings.ambientIntensity:F2}");
        sb.AppendLine($"   Sun:                 {(RenderSettings.sun == null ? "не назначен" : RenderSettings.sun.name)}");
        sb.AppendLine($"   Reflection Intensity:{RenderSettings.reflectionIntensity:F2}");
        sb.AppendLine("[Свет] ---- ИСТОЧНИКИ В СЦЕНЕ ----");

        foreach (var l in Object.FindObjectsOfType<Light>())
        {
            string warn = "";
            if (l.type == LightType.Directional && l.lightmapBakeType == LightmapBakeType.Baked)
                warn = "  <== BAKED: если свет не запечён — ничего не светит";
            if (l.intensity < 0.5f) warn += "  <== очень слабая";
            sb.AppendLine($"   '{l.name}'  type={l.type}  bake={l.lightmapBakeType}  " +
                          $"intensity={l.intensity:F2}  shadows={l.shadows}{warn}");
        }

        Debug.Log(sb.ToString());
    }


    // ---------- 9. Починить освещение ----------
    [MenuItem("Tools/Voxel/9. Починить освещение сцены", priority = 9)]
    public static void FixLighting()
    {
        var changes = new System.Text.StringBuilder();

        if (RenderSettings.skybox == null)
        {
            RenderSettings.skybox =
                AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            changes.AppendLine("   + поставлен скайбокс по умолчанию");
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.2f;
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        changes.AppendLine("   + Ambient Mode = Skybox, Intensity 1.2");
        changes.AppendLine("   + Ambient Source = Skybox (свет идёт от неба, а не от цвета)");

        var dir = Object.FindObjectsOfType<Light>()
            .FirstOrDefault(l => l.type == LightType.Directional);
        if (dir == null)
        {
            var go = new GameObject("Directional Light");
            dir = go.AddComponent<Light>();
            dir.type = LightType.Directional;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            changes.AppendLine("   + создан Directional Light");
        }

        if (dir.lightmapBakeType == LightmapBakeType.Baked)
        {
            dir.lightmapBakeType = LightmapBakeType.Realtime;
            changes.AppendLine("   + направленный свет: Baked -> Realtime");
        }

        if (dir.intensity < 1f) dir.intensity = 1.2f;
        changes.AppendLine($"   + интенсивность направленного света = {dir.intensity:F2}");

        var fill = Object.FindObjectsOfType<Light>().FirstOrDefault(l => l.name == "Fill Light");
        if (fill == null)
        {
            var go = new GameObject("Fill Light");
            fill = go.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.4f;
            fill.color = new Color(0.75f, 0.82f, 1f);
            fill.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(20f, 160f, 0f);
            changes.AppendLine("   + создан мягкий 'Fill Light' (удали, если не нужен)");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[VoxelTools] Освещение обновлено:\n{changes}\nСмотри Game view.");
    }


    // ================= применение настроек к материалу =================
    static void ApplyTo(Material mat, string hex)
    {
        Color lin = HexToLinear(hex);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lin);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", lin);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);   // металл всегда 0
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", SmoothnessFor(hex));

        if (mat.HasProperty("_EmissionColor"))
        {
            if (IsNeon(hex))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", lin * EmissionStrength);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }

        EditorUtility.SetDirty(mat);
    }

    static bool IsNeon(string hex) => NeonHexes.Any(n => n.ToUpper() == hex.ToUpper());


    // ================= служебное =================
    class Group
    {
        public readonly List<int> Tris = new List<int>();
        public readonly List<Vector3> Pts = new List<Vector3>();
        public float Area;

        public Vector3 Center()
        {
            if (Pts.Count == 0) return Vector3.zero;
            Vector3 s = Vector3.zero;
            foreach (var p in Pts) s += p;
            return s / Pts.Count;
        }

        public Vector3 Size()
        {
            if (Pts.Count == 0) return Vector3.zero;
            Vector3 mn = Pts[0], mx = Pts[0];
            foreach (var p in Pts)
            {
                mn = Vector3.Min(mn, p);
                mx = Vector3.Max(mx, p);
            }
            return mx - mn;
        }
    }

    static Dictionary<string, Group> GroupTris(int[] tris, Vector2[] uvs, Color[] px, int w, int h, Vector3[] verts)
    {
        var groups = new Dictionary<string, Group>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i], b = tris[i + 1], c = tris[i + 2];
            Vector2 uv = (uvs[a] + uvs[b] + uvs[c]) / 3f;
            string hex = NearestPaletteHex(Sample(px, w, h, uv));

            if (!groups.TryGetValue(hex, out var g))
            {
                g = new Group();
                groups[hex] = g;
            }

            g.Tris.Add(a); g.Tris.Add(b); g.Tris.Add(c);
            g.Area += Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]).magnitude * 0.5f;
            g.Pts.Add((verts[a] + verts[b] + verts[c]) / 3f);
        }

        return groups;
    }

    static Color32 Sample(Color[] px, int w, int h, Vector2 uv)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(uv.x * (w - 1)), 0, w - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(uv.y * (h - 1)), 0, h - 1);
        return px[y * w + x];
    }

    static string NearestPaletteHex(Color32 c)
    {
        string best = PaletteHexes[0];
        int bestD = int.MaxValue;
        foreach (var hex in PaletteHexes)
        {
            var p = HexToColor32(hex);
            int dr = c.r - p.r, dg = c.g - p.g, db = c.b - p.b;
            int d = dr * dr + dg * dg + db * db;
            if (d < bestD) { bestD = d; best = hex.ToUpper(); }
        }
        return best;
    }

    static bool TryGetSelected(out MeshFilter mf, out MeshRenderer mr)
    {
        mf = null; mr = null;

        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Voxel Tools", "Сначала выбери объект модели в Hierarchy.", "Ок");
            return false;
        }

        mf = go.GetComponent<MeshFilter>() ?? go.GetComponentInChildren<MeshFilter>();
        if (mf == null)
        {
            EditorUtility.DisplayDialog("Voxel Tools", "У объекта нет MeshFilter.", "Ок");
            return false;
        }

        mr = mf.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            EditorUtility.DisplayDialog("Voxel Tools", "У меша нет MeshRenderer.", "Ок");
            return false;
        }
        return true;
    }

    static Texture2D GetBaseTexture(Material m)
    {
        if (m == null) return null;
        Texture2D t = null;
        if (m.HasProperty("_BaseMap")) t = m.GetTexture("_BaseMap") as Texture2D;
        if (t == null && m.HasProperty("_MainTex")) t = m.GetTexture("_MainTex") as Texture2D;
        return t;
    }

    static Color[] ReadPixels(Texture2D src)
    {
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.filterMode = FilterMode.Point;

        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        var tmp = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0, false);
        tmp.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var px = tmp.GetPixels();
        Object.DestroyImmediate(tmp);
        return px;
    }

    static Color32 HexToColor32(string hex)
    {
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(0, 0, 0, 255);
    }

    // sRGB -> linear. Без этого цвета вымываются.
    static Color HexToLinear(string hex) => ((Color)HexToColor32(hex)).linear;

    // "Mat_9CFF3D" или "Mat_9CFF3D 1" -> "9CFF3D"
    static string HexFromName(string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(name, "([0-9A-Fa-f]{6})");
        return m.Success ? m.Value.ToUpper() : null;
    }
}
