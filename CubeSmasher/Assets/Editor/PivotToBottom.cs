// Assets/Editor/PivotToBottom.cs
// ћеню: Tools -> VoxSplit -> ѕивот в низ
// —двигает вершины мешей так, чтобы низ геометрии попал в 0 по Y.
// ћеш Ч это .asset, поэтому все префабы и объекты сцены обнов€тс€ сами.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PivotToBottom
{
    // false = машины опуст€тс€ колЄсами на Y=0 (обычно это и нужно)
    // true  = объекты останутс€ визуально на месте (если уже расставлены)
    const bool KeepWorldPosition = false;

    [MenuItem("Tools/VoxSplit/ѕивот в низ Ч выделенные")]
    static void DoSelected()
    {
        var filters = new List<MeshFilter>();
        foreach (GameObject go in Selection.gameObjects)
            filters.AddRange(go.GetComponentsInChildren<MeshFilter>(true));
        Run(filters);
    }

    [MenuItem("Tools/VoxSplit/ѕивот в низ Ч все в сцене")]
    static void DoAll()
    {
        var filters = new List<MeshFilter>(
            Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None));
        Run(filters);
    }

    static void Run(List<MeshFilter> filters)
    {
        // меш общий у многих объектов Ч считаем сдвиг один раз
        var computed = new Dictionary<Mesh, float>();
        var objects = new List<(Transform t, float shift)>();
        int meshesChanged = 0;

        foreach (MeshFilter mf in filters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            if (!computed.TryGetValue(mesh, out float shift))
            {
                Vector3[] v = mesh.vertices;
                if (v.Length == 0) { computed[mesh] = 0f; continue; }

                float minY = float.MaxValue;
                for (int i = 0; i < v.Length; i++)
                    if (v[i].y < minY) minY = v[i].y;

                // уже в нуле Ч второй прогон ничего не сделает
                if (Mathf.Abs(minY) < 1e-4f) { computed[mesh] = 0f; continue; }

                shift = -minY;
                for (int i = 0; i < v.Length; i++) v[i].y += shift;

                Undo.RecordObject(mesh, "Pivot To Bottom");
                mesh.vertices = v;
                mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);
                computed[mesh] = shift;
                meshesChanged++;
            }

            if (shift != 0f) objects.Add((mf.transform, shift));
        }

        if (KeepWorldPosition)
        {
            foreach ((Transform t, float shift) in objects)
            {
                Undo.RecordObject(t, "Pivot To Bottom");
                t.position -= t.up * (shift * t.lossyScale.y);
                EditorUtility.SetDirty(t);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Pivot] ћешей сдвинуто: {meshesChanged}, объектов: {objects.Count}. " +
                  (KeepWorldPosition ? "ѕозици€ в мире сохранена." : "ќбъекты опущены колЄсами на Y=0."));
    }
}