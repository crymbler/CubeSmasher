// VoxSplitter.cs (v2) — разбивает .vox из MagicaVoxel по цветам палитры
// КУДА ПОЛОЖИТЬ: Assets/Editor/VoxSplitter.cs  (заменить старый VoxSplitter.cs целиком)
// Меню: Tools -> VoxSplit
//
// Порядок:
//   1. Выдели .vox в Project -> "1. Разбить выбранный .vox по цветам"
//   2. Выдели машину в Hierarchy -> "2. Применить к выбранному объекту"

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class VoxSplitter
{
    const float EmissionStrength = 1.5f;
    const float GlassAlpha = 0.35f;
    // Масштаб как у NativeVoxImporter по умолчанию (0.1), чтобы машина
    // совпадала по размеру с остальными voxel-объектами проекта.
    const float VoxelScale = 0.1f;

    // Цвета палитры MagicaVoxel (HEX), которые светятся или прозрачные
    static readonly HashSet<string> NeonHex = new HashSet<string> { "ED701C", "D24545", "F7D578" };
    static readonly HashSet<string> GlassHex = new HashSet<string> { "4A8096" };

    const string KeyFolder = "VoxSplit.Folder";
    const string KeyMesh = "VoxSplit.Mesh";
    const string KeyMats = "VoxSplit.Mats";

    // ---------- 1. Разбить .vox ----------
    [MenuItem("Tools/VoxSplit/1. Разбить выбранный .vox по цветам")]
    public static void SplitSelectedVox()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".vox", System.StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("VoxSplit", "Выбери в Project сам .vox-файл.", "Ок");
            return;
        }

        var vox = VoxFileReader.Read(path);
        if (vox == null || vox.Models.Count == 0)
        {
            Debug.LogError($"[VoxSplit] Не удалось прочитать {path}");
            return;
        }

        var model = vox.Models[0];
        Debug.Log($"[VoxSplit] Файл: {path}. Модель 0: {model.Size.x}x{model.Size.y}x{model.Size.z}, вокселей {model.Voxels.Count}");

        BuildAndSave(model, vox.Palette, path);
    }

    static void BuildAndSave(VoxModelData model, Color32[] palette, string voxPath)
    {
        // Видимые грани, сгруппированные по индексу цвета
        var facesByColor = new Dictionary<int, List<Quad>>();
        var occupied = new Dictionary<(int, int, int), int>();
        foreach (var v in model.Voxels) occupied[(v.x, v.y, v.z)] = v.color;

        foreach (var v in model.Voxels)
        {
            bool vGlass = IsGlassColor(palette[v.color - 1]);
            for (int f = 0; f < 6; f++)
            {
                var nb = Neighbour(v.x, v.y, v.z, f);
                if (occupied.TryGetValue(nb, out int nColor))
                {
                    // Сосед занят. Грань рисуем только у корпуса, когда сосед — стекло.
                    // Так стык стекла с корпусом глухой, а через стекло видна только пустота.
                    bool nGlass = IsGlassColor(palette[nColor - 1]);
                    bool emit = !vGlass && nGlass;
                    if (!emit) continue;
                }

                if (!facesByColor.TryGetValue(v.color, out var list))
                {
                    list = new List<Quad>();
                    facesByColor[v.color] = list;
                }
                list.Add(new Quad { x = v.x, y = v.y, z = v.z, face = f });
            }
        }

        if (facesByColor.Count == 0)
        {
            Debug.LogError("[VoxSplit] Нет видимых граней.");
            return;
        }

        string dir = Path.GetDirectoryName(voxPath).Replace('\\', '/');
        string baseName = Path.GetFileNameWithoutExtension(voxPath);
        string folder = $"{dir}/{baseName}_split";
        if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
        AssetDatabase.CreateFolder(dir, baseName + "_split");

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Непрозрачные первыми, стекло последним
        var colors = facesByColor.Keys
            .OrderBy(ci => IsGlassColor(palette[ci - 1]) ? 1 : 0)
            .ThenByDescending(ci => facesByColor[ci].Count)
            .ToList();

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var subs = new List<int>[colors.Count];
        for (int s = 0; s < colors.Count; s++)
        {
            var sub = new List<int>();
            foreach (var q in facesByColor[colors[s]])
                AddQuad(q, model.Size, verts, normals, sub);
            subs[s] = sub;
        }

        var mesh = new Mesh { name = baseName + "_mesh", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.subMeshCount = colors.Count;
        for (int s = 0; s < colors.Count; s++)
            mesh.SetTriangles(subs[s], s);
        mesh.RecalculateBounds();

        var report = new StringBuilder();
        var matPaths = new List<string>();
        var mats = new List<Material>();
        for (int s = 0; s < colors.Count; s++)
        {
            int ci = colors[s];
            Color32 c = palette[ci - 1];
            string hex = ColorUtility.ToHtmlStringRGB(c);

            var mat = new Material(shader) { name = $"Mat_{hex}_idx{ci}" };
            ApplyColor(mat, c);
            string matPath = $"{folder}/{mat.name}.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            matPaths.Add(matPath);
            mats.Add(mat);

            report.AppendLine($"   #{hex}  индекс {ci,3}  граней: {facesByColor[ci].Count,6}" +
                              (IsGlassColor(c) ? "  <СТЕКЛО>" : "") +
                              (IsNeonColor(c) ? "  <СВЕТ>" : ""));
        }

        string meshPath = $"{folder}/{mesh.name}.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Запоминаем пути, чтобы пункт 2 брал ровно эти ассеты
        EditorPrefs.SetString(KeyFolder, folder);
        EditorPrefs.SetString(KeyMesh, meshPath);
        EditorPrefs.SetString(KeyMats, string.Join("|", matPaths));

        Debug.Log($"[VoxSplit] Готово: {mats.Count} материалов, мешей 1, папка {folder}\n{report}" +
                  "Теперь выдели машину в Hierarchy и запусти пункт 2.");
    }

    // ---------- 2. Применить к выбранному объекту ----------
    [MenuItem("Tools/VoxSplit/2. Применить к выбранному объекту")]
    public static void ApplyLastToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("VoxSplit", "Выдели объект модели в Hierarchy.", "Ок");
            return;
        }

        var mf = go.GetComponent<MeshFilter>() ?? go.GetComponentInChildren<MeshFilter>();
        var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
        if (mf == null || mr == null)
        {
            EditorUtility.DisplayDialog("VoxSplit", "У объекта нет MeshFilter или MeshRenderer.", "Ок");
            return;
        }

        string meshPath = EditorPrefs.GetString(KeyMesh, "");
        string matsRaw = EditorPrefs.GetString(KeyMats, "");
        if (string.IsNullOrEmpty(meshPath) || string.IsNullOrEmpty(matsRaw))
        {
            EditorUtility.DisplayDialog("VoxSplit", "Сначала запусти пункт 1 (разбить .vox).", "Ок");
            return;
        }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        var mats = matsRaw.Split('|')
            .Select(p => AssetDatabase.LoadAssetAtPath<Material>(p))
            .ToArray();

        if (mesh == null || mats.Length == 0 || mats.Any(m => m == null))
        {
            EditorUtility.DisplayDialog("VoxSplit",
                "Не нашёл меш или материалы. Запусти пункт 1 заново.", "Ок");
            return;
        }

        if (mesh.subMeshCount != mats.Length)
        {
            Debug.LogError($"[VoxSplit] Не совпадает: сабмешей {mesh.subMeshCount}, материалов {mats.Length}. Запусти пункт 1 заново.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(new Object[] { mf, mr }, "VoxSplit Apply");
        mf.sharedMesh = mesh;
        mr.sharedMaterials = mats;
        EditorUtility.SetDirty(mf);
        EditorUtility.SetDirty(mr);

        Debug.Log($"[VoxSplit] Применено: меш и {mats.Length} материалов на '{go.name}'.");
    }

    // ---------- Материал ----------
    static void ApplyColor(Material mat, Color32 c)
    {
        Color lin = ((Color)c).linear;
        bool glass = IsGlassColor(c);
        bool neon = IsNeonColor(c);

        lin.a = glass ? GlassAlpha : 1f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lin);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", lin);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", glass ? 0.9f : 0.35f);

        if (glass) MakeTransparent(mat);

        if (mat.HasProperty("_EmissionColor"))
        {
            if (neon)
            {
                Color e = ((Color)c).linear * EmissionStrength;
                e.a = 1f;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", e);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
        EditorUtility.SetDirty(mat);
    }

    static void MakeTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    static bool IsGlassColor(Color32 c) => GlassHex.Contains(ColorUtility.ToHtmlStringRGB(c));
    static bool IsNeonColor(Color32 c) => NeonHex.Contains(ColorUtility.ToHtmlStringRGB(c));

    // ---------- Геометрия ----------
    struct Quad { public int x, y, z, face; }

    // Грани: 0:-X 1:+X 2:-Y 3:+Y 4:-Z 5:+Z (координаты vox)
    static (int, int, int) Neighbour(int x, int y, int z, int f)
    {
        switch (f)
        {
            case 0: return (x - 1, y, z);
            case 1: return (x + 1, y, z);
            case 2: return (x, y - 1, z);
            case 3: return (x, y + 1, z);
            case 4: return (x, y, z - 1);
            default: return (x, y, z + 1);
        }
    }

    // Квад грани: 2 треугольника. vox (x,y,z) -> unity (x, z, y).
    // Перестановка Y/Z зеркалит меш, поэтому обход треугольников развёрнут (проверено).
    static void AddQuad(Quad q, Vector3Int size, List<Vector3> verts, List<Vector3> normals, List<int> tris)
    {
        Vector3 o = new Vector3(q.x, q.y, q.z);
        Vector3[] c;
        Vector3 n;
        switch (q.face)
        {
            case 0: c = new[] { o + new Vector3(0, 0, 0), o + new Vector3(0, 0, 1), o + new Vector3(0, 1, 1), o + new Vector3(0, 1, 0) }; n = new Vector3(-1, 0, 0); break;
            case 1: c = new[] { o + new Vector3(1, 0, 0), o + new Vector3(1, 1, 0), o + new Vector3(1, 1, 1), o + new Vector3(1, 0, 1) }; n = new Vector3(1, 0, 0); break;
            case 2: c = new[] { o + new Vector3(0, 0, 0), o + new Vector3(1, 0, 0), o + new Vector3(1, 0, 1), o + new Vector3(0, 0, 1) }; n = new Vector3(0, -1, 0); break;
            case 3: c = new[] { o + new Vector3(0, 1, 0), o + new Vector3(0, 1, 1), o + new Vector3(1, 1, 1), o + new Vector3(1, 1, 0) }; n = new Vector3(0, 1, 0); break;
            case 4: c = new[] { o + new Vector3(0, 0, 0), o + new Vector3(0, 1, 0), o + new Vector3(1, 1, 0), o + new Vector3(1, 0, 0) }; n = new Vector3(0, 0, -1); break;
            default: c = new[] { o + new Vector3(0, 0, 1), o + new Vector3(1, 0, 1), o + new Vector3(1, 1, 1), o + new Vector3(0, 1, 1) }; n = new Vector3(0, 0, 1); break;
        }

        int b = verts.Count;
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = c[i];
            // центрируем по модели, переводим в Unity-оси и масштабируем
            float ux = (p.x - size.x * 0.5f) * VoxelScale;
            float uy = (p.z - size.z * 0.5f) * VoxelScale;
            float uz = (p.y - size.y * 0.5f) * VoxelScale;
            verts.Add(new Vector3(ux, uy, uz));
            normals.Add(new Vector3(n.x, n.z, n.y));
        }
        tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
        tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
    }

    // ---------- Чтение .vox ----------
    class VoxModelData
    {
        public Vector3Int Size;
        public List<VoxelData> Voxels = new List<VoxelData>();
    }
    struct VoxelData { public int x, y, z, color; }
    class VoxFileData
    {
        public List<VoxModelData> Models = new List<VoxModelData>();
        public Color32[] Palette = new Color32[256];
    }

    static class VoxFileReader
    {
        public static VoxFileData Read(string assetPath)
        {
            byte[] data = File.ReadAllBytes(assetPath);
            if (data.Length < 20 || data[0] != 'V' || data[1] != 'O' || data[2] != 'X' || data[3] != ' ')
                return null;

            var file = new VoxFileData();
            for (int i = 0; i < 256; i++) file.Palette[i] = new Color32(255, 255, 255, 255);

            VoxModelData current = null;
            int off = 20;   // 'VOX ' (4) + version (4) + 'MAIN' chunk header (12)

            while (off + 12 <= data.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(data, off, 4);
                int contentSize = System.BitConverter.ToInt32(data, off + 4);
                int childrenSize = System.BitConverter.ToInt32(data, off + 8);
                int body = off + 12;

                if (id == "SIZE")
                {
                    current = new VoxModelData
                    {
                        Size = new Vector3Int(
                            System.BitConverter.ToInt32(data, body),
                            System.BitConverter.ToInt32(data, body + 4),
                            System.BitConverter.ToInt32(data, body + 8))
                    };
                    file.Models.Add(current);
                }
                else if (id == "XYZI" && current != null)
                {
                    int n = System.BitConverter.ToInt32(data, body);
                    for (int i = 0; i < n; i++)
                    {
                        int p = body + 4 + i * 4;
                        current.Voxels.Add(new VoxelData
                        {
                            x = data[p],
                            y = data[p + 1],
                            z = data[p + 2],
                            color = data[p + 3]
                        });
                    }
                }
                else if (id == "RGBA")
                {
                    // Индекс вокселя v -> палитра[v-1]
                    for (int i = 0; i < 256; i++)
                    {
                        int p = body + i * 4;
                        file.Palette[i] = new Color32(data[p], data[p + 1], data[p + 2], data[p + 3]);
                    }
                }

                off = body + contentSize + childrenSize;
            }

            return file;
        }
    }
}
