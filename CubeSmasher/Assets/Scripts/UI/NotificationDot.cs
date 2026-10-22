using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Красный кружок-уведомление. Создаётся кодом, а не лежит в префабе.
///
/// Почему так: HUD и экран гаража верстаются руками, и правка префабов расходится
/// с кодом. Здесь кружок появляется сам у нужной кнопки — ни префаб, ни сцена
/// не требуют назначений в инспекторе.
///
/// Если объект с таким именем уже есть (сделан вручную), он переиспользуется.
/// </summary>
public static class NotificationDot
{
    public const float Size = 27f;

    /// <summary>
    /// Вернуть кружок у родителя, создав его при отсутствии.
    /// Кружок выключен: включает его NotificationDots, когда есть что сообщить.
    /// </summary>
    public static GameObject Ensure(Transform parent, string name = "Dot",
                                    Vector2? offset = null, float size = Size)
    {
        if (parent == null) return null;

        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = offset ?? new Vector2(-3f, -3f);
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling(); // поверх иконки кнопки, иначе кружок уедет под неё

        var img = go.GetComponent<Image>();
        img.color = CubeSmasher.UI.UITheme.Notification;
        img.raycastTarget = false; // клик должен проходить в кнопку под кружком

        // Тёмная окантовка: кружок читается на любом фоне кнопки
        var rim = go.AddComponent<Outline>();
        rim.effectColor = CubeSmasher.UI.UITheme.NotificationRim;
        rim.effectDistance = new Vector2(2f, -2f);

        go.SetActive(false);
        return go;
    }
}
