/*
================================================================================
 VoxSplitter — почему здесь всё сделано именно так
================================================================================
 Разбирались три ошибки подряд. Все три давали один и тот же визуальный симптом
 («на стыке стекла и кузова что-то не так»), но причины были разные. Ниже — что
 было сломано, по каким цифрам это видно и как проверить, что не повторилось.

--------------------------------------------------------------------------------
 0. ЭТАЛОН, С КОТОРЫМ ВСЁ СВЕРЯЕТСЯ
--------------------------------------------------------------------------------
 Car1/CubeSmasherCars.vox  — рабочая машина, вид которой устраивает.
 Её характеристики (из инспектора и из файла):

   SIZE                    46 x 32 x 24
   вокселей                15806
   квадов                  7880
   треугольников           15760   (= квадов * 2)
   сабмешей                12
   стекло в палитре        индекс 227, #4A8096, _type = _glass
   свечение в палитре      209 #ED701C, 217 #F7D578, 222 #D24545, _type = _emit

 Именно 15760 и 12 — контрольное число. Любой прогон на этом .vox,
 дающий другие числа, содержит баг. Проверять НУЖНО по числу вокселей:
 у каждой машины оно своё, у Car1 — 15806.

--------------------------------------------------------------------------------
 1. ОШИБКА ПЕРВАЯ: SeamInset  (давала щель по контуру окна)
--------------------------------------------------------------------------------
 Что я сделал: стыковую грань кузова «утапливал» внутрь на 0.01–0.02 вокселя,
 чтобы она не спорила по глубине со стеклом (_SeamInset = 0.02f).

 Почему это неверно: в воксельном меше плоскости СОВПАДАЮТ по построению, и
 это правильно. Отодвигая одну из них, ты не убираешь z-fighting, а создаёшь
 зазор ровно по периметру окна. Через прозрачное стекло в этот зазор видна
 внутренность машины — тонкая тёмная рваная полоса по рамке.

 Как распознать: полоса повторяет контур окна, имеет постоянную толщину и
 НЕ ИСЧЕЗАЕТ при изменении настроек материала.

 Вывод: никаких сдвигов и отступов у граней быть не может. Координаты вершин
 строго целочисленные по сетке вокселей, плоскость у каждой грани ровно на
 границе вокселя. В текущем коде AddQuad() не имеет параметра inset вообще —
 это защита от рецидива, не добавляй его обратно.

--------------------------------------------------------------------------------
 2. ОШИБКА ВТОРАЯ: грань стыка отдана стеклу  (давала сквозной вид)
--------------------------------------------------------------------------------
 В рабочем v2 правило было такое (и оно верное):

     если сосед занят:
         рисуем грань ТОЛЬКО когда я кузов (!vGlass), а сосед — стекло (nGlass)
         во всех остальных случаях — пропускаем

     bool emitFace = !vGlass && nGlass;

 То есть плоскость стыка принадлежит КУЗОВУ. Кузов непрозрачный, пишет
 глубину (ZWrite = 1), и за каждым стеклянным вокселем стоит глухая стенка.
 Сквозь стекло видно стенку, а не внутренности машины.

 В одной из версий я сделал наоборот — грань стыка рисовало СТЕКЛО. Прозрачная
 плоскость без ZWrite встала туда, где должна быть стенка, и всё стало видно
 насквозь. Плюс эти грани лежали ВНУТРИ стоек кузова и, так как они прозрачные,
 смешивались поверх непрозрачной геометрии — отсюда «полосы поверх стойки» и
 ощущение, что «материалы воюют за отрисовку». Никто не воевал: это были лишние
 треугольники стеклянного сабмеша, которых в рабочей модели просто нет.

 По цифрам для Car1:
     правильно (грань у кузова):  15760 треугольников, стенок у стекла 256
     неправильно (грань у стекла): 15248 треугольников, стенок 0
     разница 512 треугольников = 256 квадов — это ровно стенки окон.

 Как распознать: в логе «стенок у стекла 0» при наличии стекла.
 Или у #525F7A в модели на 15806 вокселей стоит 2880 вместо 3136.

 Вывод: полная таблица владения гранями, других вариантов быть не должно:

     кузов  <-> пустота     : рисует кузов, обычная грань
     кузов  <-> кузов       : никто (грань внутри, не видна)
     кузов  <-> стекло      : рисует КУЗОВ (глухая стенка за стеклом)
     стекло <-> стекло      : никто
     стекло <-> пустота     : рисует стекло (только наружная оболочка)

 Стекло НИКОГДА не рисует грань в сторону кузова. Оно рисует только наружу.
 Поэтому у стеклянного сабмеша граней всегда относительно мало: у Car1 стекло
 #4A8096 = 1640 из 7880, а кузов #525F7A = 3136.

--------------------------------------------------------------------------------
 3. ОШИБКА ТРЕТЬЯ (главная): парсер MATL не читал формат .vox
--------------------------------------------------------------------------------
 Чанк MATL в .vox — БИНАРНЫЙ, а не текст «ключ:значение через запятую»:

     int32   индекс палитры (1..255)
     int32   количество пар
     повторить N раз:
         int32 длина_ключа
         bytes ключ   (UTF8)
         int32 длина_значения
         bytes значение (UTF8)

 Я делил содержимое по запятой, поэтому '_type' не находился НИКОГДА.
 Но это ещё только половина беды. Вторая половина — вот эта строка старого
 кода в fallback-е:

     if (vox.Props[i].fromFile) continue;

 MagicaVoxel пишет MATL на ВСЕ 256 индексов палитры (даже для обычных цветов).
 Значит fromFile становился true у каждого цвета, и запасная проверка по hex
 (#4A8096 -> стекло) не выполнялась ни для одного. Итог: стекла в модели
 НЕТ ВООБЩЕ, правило из п.2 не срабатывает ни разу, стенок 0, всё просвечивает.

 Важно: в Car1 стекло НЕ помечено прозрачным по альфе палитры — только через
 _type = _glass в MATL. Поэтому определять стекло по цвету в принципе
 ненадёжно, hex-список должен быть лишь аварийным вариантом.

 Вывод: MATL читается бинарно (ParseMatl), а признак hasType ставится ТОЛЬКО
 когда '_type' реально найден в данных. Fallback по hex применяется лишь там,
 где hasType == false. В логе первой строкой печатается, что найдено:

     стекло: 227/#4A8096 | свечение: 209/#ED701C, 217/#F7D578, 222/#D24545

 Если тут «не найдено» — парсер сломан, дальше смотреть смысла нет.

--------------------------------------------------------------------------------
 4. МАТЕРИАЛ СТЕКЛА: его нельзя «настроить», его надо копировать
--------------------------------------------------------------------------------
 Рабочий Mat_4A8096_idx227.mat из Car1 использует ПРЕМНОЖЕННОЕ смешивание,
 а не обычное Alpha:

     _SrcBlend: 1 (One)          _DstBlend: 10 (OneMinusSrcAlpha)
     _SrcBlendAlpha: 1           _DstBlendAlpha: 10
     _ALPHAPREMULTIPLY_ON        (а _ALPHABLEND_ON — в InvalidKeywords)
     _ZWrite: 0                  _Surface: 1
     _BaseColor alpha: 0.35      _Smoothness: 0.9
     CustomRenderQueue: 3000     RenderType: Transparent
     disabledShaderPasses: DepthOnly, SHADOWCASTER
     _Cull: 2 (Back)

 Если настроить стекло «руками по памяти» как SrcAlpha / _ALPHABLEND_ON, режим
 смешивания будет другим, и даже без дыр вид отличается: край окна грязнее,
 тёмные тона проваливаются, на стыке заметна разница.

 Отсюда решение: материал НЕ конфигурируется с нуля, а берётся new Material(src),
 где src — материал из Car1, найденный по hex в имени (Mat_XXXXXX_...).
 Копия переносит шейдер, все ключи, очередь, отключённые пассы и цвета — совпасть
 обязательно. Собственная настройка (ConfigureFallback) применяется только к
 цветам, которых в Car1 нет.

 Отдельно: _Cull = Back, НЕ Both. Меш замкнутый по построению, двойная сторона
 только добавит смешивания с внутренними гранями и снова даст грязь на рамке.

--------------------------------------------------------------------------------
 5. МАТЕРИАЛЫ: 42 цвета на 17 машин, а не 12 на каждую
--------------------------------------------------------------------------------
 Ошибка была в том, что материалы создавались в папке КАЖДОЙ модели
 (<baseName>_split/Mat_XXXXXX), поэтому один и тот же цвет размножался по 17 раз.
 Теперь одна общая папка:

     Assets/Prefabs/Voxel/Cars/Materials/Mat_<HEX>[_G][_E][_M].mat

 имя = только hex + суффиксы типа, без индекса и без номера модели. Если файл
 уже существует — он переиспользуется. Итого на весь проект максимум 42 файла,
 сколько бы раз ни перезапускался скрипт.

 ВАЖНО про перезапуски: RrebuildMaterials = true пересоздаёт материалы копией
 из Car1 (ручные правки затрутся). После того как стекло и фары настроены и
 устраивают — поставь false, иначе правки будут теряться при каждом прогоне.

--------------------------------------------------------------------------------
 6. ГЕОМЕТРИЯ: оси, масштаб, пивот
--------------------------------------------------------------------------------
 Координаты .vox -> Unity:  (x, y, z) -> (x, z, y)
     потому что в MagicaVoxel Z — это «вверх», а в Unity вверх — Y.
     Перестановка зеркалит меш, поэтому обход треугольников развёрнут:
         b+0, b+2, b+1   и   b+0, b+3, b+2

 Нормали переставляются так же: (n.x, n.z, n.y).

 Масштаб VoxelScale = 0.1f — как у NativeVoxImporter по умолчанию и как в Car1.

 Пивот: BottomPivot = false повторяет Car1, где bounds center = (0.05, 0, 0),
 то есть центр по всем осям. Если переключить на true, меш получает пивот по
 низу колёс, но тогда Car1 надо перегенерить ТЕМ ЖЕ скриптом, иначе он один
 останется с центровым пивотом и встанет на полмашины в землю.

--------------------------------------------------------------------------------
 7. РЕГЛАМЕНТ ПРОВЕРКИ (делать после каждого прогона)
--------------------------------------------------------------------------------
 1) Первая строка лога: найдены ли стекло и свечение.
       стекло: 227/#4A8096 | свечение: 209/#ED701C, 217/#F7D578, 222/#D24545
    «не найдено» = парсер MATL сломан, остальное не проверяем.

 2) У модели с 15806 вокселями (Car1):
       сабмешей            12
       треугольников       15760
       стенок у стекла     256
       #525F7A             3136
    Должно совпасть все четыре числа. Это гарантия, что геометрия новых машин
    идентична рабочей.

 3) «стенок у стекла 0» у любой модели с стеклом = баг правила граней
    (п.2) или у той машины стекло не помечено в палитре MagicaVoxel.

 4) Материалов в общей папке не больше 42.

 5) Визуально, вплотную к рамке окна:
       ровная тёмная полоса по контуру  -> это стенка кузова за стеклом, норма
       рваная полоса зубцами по вокселям -> Render Face = Both или ZWrite = 1
       полоса заметна сквозь стекло в пустоту -> нет стенок, п.2

--------------------------------------------------------------------------------
 8. ЧЕГО НЕ ДЕЛАТЬ (список выученных грабель)
--------------------------------------------------------------------------------
 - Не сдвигать грани относительно сетки вокселей (SeamInset, любой оффсет).
 - Не отдавать грань стыка стеклу; плоскость стыка всегда у кузова.
 - Не определять стекло только по hex: смотреть _type в MATL.
 - Не ставить hasType = true, если '_type' не найден (иначе fallback мёртв).
 - Не собирать материал стекла вручную; копировать из Car1 (Premultiply).
 - Не ставить стеклу Render Face = Both и ZWrite = 1.
 - Не создавать материалы в папке модели; только общая папка, имя = hex.
 - Не включать стеклу ShadowCaster и DepthOnly (в Car1 они отключены).
 - Не экспортировать из MagicaVoxel по одной машине: File -> Export пишет весь
   мир; скрипт читает ВСЕ модели из одного .vox, включая группы и слои.
================================================================================
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class VoxSplitter
{
    // ---------- настройки ----------
    const float VoxelScale = 0.1f;
    const float GlassAlpha = 0.35f;   // как в Car1
    const float EmitStrength = 1.5f;
    const bool BottomPivot = false;
    const float SpawnSpacing = 3f;

    // true  = при каждом прогоне материалы пересоздаются копией из Car1
    //         (ручные правки затрутся — включай для чистого старта)
    // false = существующие .mat переиспользуются как есть (правки сохраняются)
    const bool RebuildMaterials = true;

    const string MaterialFolder = "Assets/Prefabs/Voxel/Cars/Materials";
    const string TemplateFolder = "Assets/Prefabs/Voxel/Cars/Car1";

    // запасной вариант, только если в .vox вообще нет _type
    static readonly HashSet<string> GlassHex = new HashSet<string> { "4A8096" };
    static readonly HashSet<string> EmitHex = new HashSet<string> { "ED701C", "D24545", "F7D578" };

    [MenuItem("Tools/VoxSplit/Разбить .vox — все модели сразу")]
    static void SplitAll()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !path.ToLowerInvariant().EndsWith(".vox"))
        { EditorUtility.DisplayDialog("VoxSplit", "Выдели в Project сам .vox-файл.", "Ок"); return; }

        Vox vox = ReadVox(path);
        if (vox == null || vox.Models.Count == 0)
        { Debug.LogError("[VoxSplit] Не прочитал " + path); return; }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("[VoxSplit] Проект не на URP."); return; }

        EnsureFolder(MaterialFolder);
        Dictionary<string, Material> tpl = LoadTemplates();
        var cache = new Dictionary<string, Material>();

        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        string baseName = Path.GetFileNameWithoutExtension(path);
        string folder = dir + "/" + baseName + "_split";

        if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
        AssetDatabase.CreateFolder(dir, baseName + "_split");

        GameObject root = GameObject.Find("/" + baseName + "_root");
        if (root == null) root = new GameObject(baseName + "_root");

        var report = new StringBuilder();
        report.AppendLine($"[VoxSplit] {Path.GetFileName(path)} — моделей: {vox.Models.Count}, " +
                          $"эталонов Car1: {tpl.Count}");
        report.AppendLine("  " + vox.DescribeMaterials());

        int spawned = 0;

        for (int mi = 0; mi < vox.Models.Count; mi++)
        {
            Model model = vox.Models[mi];
            if (model.Voxels.Count == 0) continue;

            var occ = new Dictionary<(int, int, int), int>();
            foreach (Voxel v in model.Voxels) occ[(v.x, v.y, v.z)] = v.c;

            // ================= ПРАВИЛО ГРАНЕЙ (как в v2) =================
            var buckets = new Dictionary<string, Bucket>();
            int seamWalls = 0;

            foreach (Voxel v in model.Voxels)
            {
                bool vGlass = vox.Props[v.c].glass;
                Color32 c = vox.Color(v.c);
                MatProps p = vox.Props[v.c];

                for (int f = 0; f < 6; f++)
                {
                    (int nx, int ny, int nz) = N(v.x, v.y, v.z, f);

                    if (occ.TryGetValue((nx, ny, nz), out int nc))
                    {
                        bool nGlass = vox.Props[nc].glass;

                        // грань строит только кузов и только к стеклу
                        bool emitFace = !vGlass && nGlass;
                        if (!emitFace) continue;
                        seamWalls++;
                    }

                    string hex = ColorUtility.ToHtmlStringRGB(c);
                    string key = hex + (p.glass ? "G" : "") + (p.emit ? "E" : "") + (p.metal ? "M" : "");

                    if (!buckets.TryGetValue(key, out Bucket b))
                        buckets[key] = b = new Bucket { hex = hex, props = p, color = c, faces = new List<Quad>() };

                    b.faces.Add(new Quad { x = v.x, y = v.y, z = v.z, face = f });
                }
            }
            if (buckets.Count == 0) continue;

            // корпус по убыванию граней, стекло последним
            List<string> keys = buckets.Keys
                .OrderBy(k => buckets[k].props.glass ? 1 : 0)
                .ThenByDescending(k => buckets[k].faces.Count)
                .ToList();

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var subs = new List<int>[keys.Count];
            for (int s = 0; s < keys.Count; s++)
            {
                subs[s] = new List<int>();
                foreach (Quad q in buckets[keys[s]].faces)
                    AddQuad(q, model.Size, verts, norms, subs[s]);
            }

            string meshName = baseName + "_m" + mi;
            var mesh = new Mesh { name = meshName, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.subMeshCount = keys.Count;
            for (int s = 0; s < keys.Count; s++) mesh.SetTriangles(subs[s], s);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, folder + "/" + meshName + ".asset");

            // ---------- материалы ----------
            var mats = new Material[keys.Count];
            for (int s = 0; s < keys.Count; s++)
            {
                Bucket b = buckets[keys[s]];
                string suffix = (b.props.glass ? "_G" : "") + (b.props.emit ? "_E" : "") + (b.props.metal ? "_M" : "");
                mats[s] = GetMaterial(b.hex, suffix, b.color, b.props, shader, tpl, cache);

                report.AppendLine($"   модель {mi,2}: #{b.hex}{suffix}  граней {b.faces.Count,6}" +
                                  (b.props.glass ? "  СТЕКЛО" : "") +
                                  (b.props.emit ? "  СВЕТ" : "") +
                                  (b.props.metal ? "  МЕТАЛЛ" : "") +
                                  (tpl.ContainsKey(b.hex) ? "  [копия Car1]" : ""));
            }

            // ---------- объект + префаб ----------
            var go = new GameObject(meshName);
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3((mi % 5) * SpawnSpacing, 0f, (mi / 5) * SpawnSpacing);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterials = mats;
            mr.receiveShadows = true;

            PrefabUtility.SaveAsPrefabAsset(go, folder + "/" + meshName + ".prefab");
            spawned++;

            int tris = verts.Count / 2;
            report.AppendLine($"  модель {mi}: {model.Size.x}x{model.Size.y}x{model.Size.z}, " +
                              $"вокселей {model.Voxels.Count}, сабмешей {keys.Count}, " +
                              $"треугольников {tris}, стенок у стекла {seamWalls}");

            if (seamWalls == 0 && HasGlass(buckets))
                report.AppendLine($"  !! модель {mi}: стекло есть, но стенок 0 — проверь _type в MATL");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int total = AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }).Length;
        Debug.Log(report + $"\n[VoxSplit] Готово: объектов {spawned}, " +
                  $"материалов в общей папке: {total} ({MaterialFolder})");
    }

    static bool HasGlass(Dictionary<string, Bucket> buckets)
    {
        foreach (var b in buckets.Values) if (b.props.glass) return true;
        return false;
    }

    // ---------- материалы: один файл на HEX ----------
    static Material GetMaterial(string hex, string suffix, Color32 c, MatProps p,
                                Shader shader, Dictionary<string, Material> tpl,
                                Dictionary<string, Material> cache)
    {
        string name = "Mat_" + hex + suffix;
        if (cache.TryGetValue(name, out Material cached) && cached != null) return cached;

        string matPath = MaterialFolder + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (existing != null && !RebuildMaterials) { cache[name] = existing; return existing; }
        if (existing != null) AssetDatabase.DeleteAsset(matPath);

        Material mat;
        if (tpl.TryGetValue(hex, out Material src))
        {
            mat = new Material(src);   // полная копия: шейдер, ключи, очередь, пассы, цвета
            mat.name = name;
        }
        else
        {
            mat = new Material(shader) { name = name };
            ConfigureFallback(mat, c, p);
        }

        AssetDatabase.CreateAsset(mat, matPath);
        cache[name] = mat;
        return mat;
    }

    static Dictionary<string, Material> LoadTemplates()
    {
        var map = new Dictionary<string, Material>();
        if (!AssetDatabase.IsValidFolder(TemplateFolder)) return map;

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { TemplateFolder }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null) continue;
            string hex = HexFromName(m.name);
            if (hex != null && !map.ContainsKey(hex)) map[hex] = m;
        }
        return map;
    }

    static string HexFromName(string name)
    {
        if (string.IsNullOrEmpty(name) || !name.StartsWith("Mat_") || name.Length < 10) return null;
        string h = name.Substring(4, 6).ToUpperInvariant();
        for (int i = 0; i < h.Length; i++) if (!Uri.IsHexDigit(h[i])) return null;
        return h;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // ---------- материал, если цвета нет в Car1 ----------
    static void ConfigureFallback(Material m, Color32 c, MatProps p)
    {
        Color col = c;
        if (PlayerSettings.colorSpace == ColorSpace.Linear) col = col.linear;

        var baseCol = new Color(col.r, col.g, col.b, p.glass ? GlassAlpha : 1f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseCol);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", p.metal ? 0.8f : 0f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", p.glass ? 0.9f : 0.35f);

        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", p.glass ? 1f : 0f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Back);

        if (p.glass)
        {
            // Premultiply, как в рабочем Car1
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);

            Kw(m, "_ALPHAPREMULTIPLY_ON", true);
            Kw(m, "_ALPHABLEND_ON", false);
            Kw(m, "_SURFACE_TYPE_TRANSPARENT", true);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
        }
        else
        {
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);

            Kw(m, "_SURFACE_TYPE_TRANSPARENT", false);
            Kw(m, "_ALPHAPREMULTIPLY_ON", false);
            Kw(m, "_ALPHABLEND_ON", false);
            Kw(m, "_ALPHATEST_ON", false);
            m.SetOverrideTag("RenderType", "Opaque");
            m.renderQueue = -1;
            m.SetShaderPassEnabled("DepthOnly", true);
            m.SetShaderPassEnabled("ShadowCaster", true);
        }

        if (m.HasProperty("_EmissionColor"))
        {
            Kw(m, "_EMISSION", p.emit);
            m.globalIlluminationFlags = p.emit
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            m.SetColor("_EmissionColor",
                p.emit ? new Color(col.r, col.g, col.b, 1f) * EmitStrength : Color.black);
        }
        EditorUtility.SetDirty(m);
    }

    static void Kw(Material m, string kw, bool on) { if (on) m.EnableKeyword(kw); else m.DisableKeyword(kw); }

    // ---------- геометрия ----------
    static void AddQuad(Quad q, Vector3Int size, List<Vector3> verts, List<Vector3> norms, List<int> tris)
    {
        Vector3 o = new Vector3(q.x, q.y, q.z);
        Vector3[] c; Vector3 n;
        switch (q.face)
        {
            case 0: c = new[] { o, o + new Vector3(0, 0, 1), o + new Vector3(0, 1, 1), o + new Vector3(0, 1, 0) }; n = new Vector3(-1, 0, 0); break;
            case 1: c = new[] { o + new Vector3(1, 0, 0), o + new Vector3(1, 1, 0), o + new Vector3(1, 1, 1), o + new Vector3(1, 0, 1) }; n = new Vector3(1, 0, 0); break;
            case 2: c = new[] { o, o + new Vector3(1, 0, 0), o + new Vector3(1, 0, 1), o + new Vector3(0, 0, 1) }; n = new Vector3(0, -1, 0); break;
            case 3: c = new[] { o + new Vector3(0, 1, 0), o + new Vector3(0, 1, 1), o + new Vector3(1, 1, 1), o + new Vector3(1, 1, 0) }; n = new Vector3(0, 1, 0); break;
            case 4: c = new[] { o, o + new Vector3(0, 1, 0), o + new Vector3(1, 1, 0), o + new Vector3(1, 0, 0) }; n = new Vector3(0, 0, -1); break;
            default: c = new[] { o + new Vector3(0, 0, 1), o + new Vector3(1, 0, 1), o + new Vector3(1, 1, 1), o + new Vector3(0, 1, 1) }; n = new Vector3(0, 0, 1); break;
        }

        int b0 = verts.Count;
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = c[i];
            float ux = (p.x - size.x * 0.5f) * VoxelScale;
            float uy = BottomPivot ? p.z * VoxelScale : (p.z - size.z * 0.5f) * VoxelScale;
            float uz = (p.y - size.y * 0.5f) * VoxelScale;
            verts.Add(new Vector3(ux, uy, uz));
            norms.Add(new Vector3(n.x, n.z, n.y));
        }
        tris.Add(b0); tris.Add(b0 + 2); tris.Add(b0 + 1);
        tris.Add(b0); tris.Add(b0 + 3); tris.Add(b0 + 2);
    }

    static (int, int, int) N(int x, int y, int z, int f)
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

    // ---------- данные .vox ----------
    class Vox
    {
        public List<Model> Models = new List<Model>();
        public Color32[] Palette = new Color32[256];
        public MatProps[] Props = new MatProps[256];
        public byte[] Imap;

        public Color32 Color(int i1)
        {
            int slot = i1 - 1;
            if (slot < 0 || slot > 255) slot = 0;
            int p = Imap != null ? Imap[slot] : slot;
            if (p < 0 || p > 255) p = slot;
            return Palette[p];
        }

        public string DescribeMaterials()
        {
            var glass = new List<string>();
            var emit = new List<string>();
            for (int i = 1; i < 256; i++)
            {
                if (Props[i].glass) glass.Add($"{i}/#{ColorUtility.ToHtmlStringRGB(Palette[i - 1])}");
                if (Props[i].emit) emit.Add($"{i}/#{ColorUtility.ToHtmlStringRGB(Palette[i - 1])}");
            }
            return "стекло: " + (glass.Count == 0 ? "не найдено" : string.Join(", ", glass)) +
                   " | свечение: " + (emit.Count == 0 ? "нет" : string.Join(", ", emit));
        }
    }

    class Model { public Vector3Int Size; public List<Voxel> Voxels = new List<Voxel>(); }
    struct Voxel { public int x, y, z, c; }
    struct Quad { public int x, y, z, face; }

    class MatProps
    {
        public bool glass, emit, metal, hasType;
        public float alpha = GlassAlpha;
        public float emission = EmitStrength;
    }

    class Bucket
    {
        public Color32 color;
        public MatProps props;
        public string hex;
        public List<Quad> faces;
    }

    static Vox ReadVox(string path)
    {
        byte[] d = File.ReadAllBytes(path);
        if (d.Length < 20 || d[0] != 'V' || d[1] != 'O' || d[2] != 'X' || d[3] != ' ') return null;

        var vox = new Vox();
        for (int i = 0; i < 256; i++)
        {
            vox.Palette[i] = new Color32(255, 255, 255, 255);
            vox.Props[i] = new MatProps();
        }

        Model cur = null;
        int off = 20;

        while (off + 12 <= d.Length)
        {
            string id = Encoding.ASCII.GetString(d, off, 4);
            int content = System.BitConverter.ToInt32(d, off + 4);
            int children = System.BitConverter.ToInt32(d, off + 8);
            int body = off + 12;
            if (content < 0 || body + content > d.Length) break;

            switch (id)
            {
                case "SIZE":
                    cur = new Model
                    {
                        Size = new Vector3Int(
                        System.BitConverter.ToInt32(d, body),
                        System.BitConverter.ToInt32(d, body + 4),
                        System.BitConverter.ToInt32(d, body + 8))
                    };
                    vox.Models.Add(cur);
                    break;

                case "XYZI":
                    if (cur == null) break;
                    int n = System.BitConverter.ToInt32(d, body);
                    for (int i = 0; i < n; i++)
                    {
                        int q = body + 4 + i * 4;
                        if (q + 3 >= d.Length) break;
                        cur.Voxels.Add(new Voxel { x = d[q], y = d[q + 1], z = d[q + 2], c = d[q + 3] });
                    }
                    break;

                case "RGBA":
                    for (int i = 0; i < 256; i++)
                    {
                        int q = body + i * 4;
                        if (q + 3 >= d.Length) break;
                        vox.Palette[i] = new Color32(d[q], d[q + 1], d[q + 2], d[q + 3]);
                    }
                    break;

                case "IMAP":
                    if (content >= 256)
                    {
                        vox.Imap = new byte[256];
                        System.Array.Copy(d, body, vox.Imap, 0, 256);
                    }
                    break;

                case "MATL":
                    int idx = System.BitConverter.ToInt32(d, body);
                    if (idx > 0 && idx < 256)
                        vox.Props[idx] = ParseMatl(d, body + 4, content - 4);
                    break;
            }
            off = body + content + children;
        }

        // запасной вариант только там, где _type вообще отсутствует
        for (int i = 1; i < 256; i++)
        {
            if (vox.Props[i].hasType) continue;
            string hex = ColorUtility.ToHtmlStringRGB(vox.Palette[i - 1]);
            vox.Props[i].glass = GlassHex.Contains(hex);
            vox.Props[i].emit = EmitHex.Contains(hex);
        }
        return vox;
    }

    // MATL: int32 кол-во пар, затем [len][key][len][value]...
    static MatProps ParseMatl(byte[] d, int off, int len)
    {
        var p = new MatProps();
        int end = off + len;
        if (off + 4 > end) return p;

        int pairs = System.BitConverter.ToInt32(d, off);
        off += 4;

        for (int i = 0; i < pairs && off + 4 <= end; i++)
        {
            int kl = System.BitConverter.ToInt32(d, off); off += 4;
            if (kl < 0 || off + kl > end) break;
            string k = Encoding.UTF8.GetString(d, off, kl); off += kl;

            if (off + 4 > end) break;
            int vl = System.BitConverter.ToInt32(d, off); off += 4;
            if (vl < 0 || off + vl > end) break;
            string v = Encoding.UTF8.GetString(d, off, vl); off += vl;

            switch (k)
            {
                case "_type":
                    p.hasType = true;
                    p.glass = v == "_glass";
                    p.emit = v == "_emit";
                    p.metal = v == "_metal";
                    break;

                case "_flux":
                    if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float fl))
                        p.emission = Mathf.Clamp(fl, 1f, 6f);
                    break;

                case "_alpha":
                    // хранится как «доля непрозрачности» MagicaVoxel;
                    // в Unity берём проверенное значение Car1
                    break;
            }
        }
        return p;
    }
}