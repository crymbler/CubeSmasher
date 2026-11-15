using UnityEngine;
using UnityEditor;

public static class CenterPivot
{
    [MenuItem("Tools/Pivot/Center (геометрический центр)")]
    static void Center() => Make(false);

    [MenuItem("Tools/Pivot/Bottom Center (на земле)")]
    static void Bottom() => Make(true);

    static void Make(bool bottom)
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            Vector3 pos = b.center;
            if (bottom) pos.y = b.min.y;   // XZ по центру, Y по низу

            GameObject pivot = new GameObject(go.name + "_pivot");
            Undo.RegisterCreatedObjectUndo(pivot, "Create pivot");
            pivot.transform.SetParent(go.transform.parent, false);
            pivot.transform.SetPositionAndRotation(pos, go.transform.rotation);
            go.transform.SetParent(pivot.transform, true);
        }
    }
}