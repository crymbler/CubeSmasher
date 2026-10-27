using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Подгоняет RectTransform под Screen.safeArea: вырезы, скругления и полоса жестов
    /// на телефонах. Вешается на пустой контейнер SafeArea внутри Canvas, все элементы
    /// интерфейса лежат в нём.
    ///
    /// Пересчитывается при включении, при повороте и при смене размера экрана.
    /// На платформах без вырезов (десктоп, WebGL) безопасная зона совпадает с экраном,
    /// поэтому отступы остаются нулевыми и вид не меняется.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Дополнительный отступ внутри безопасной зоны, в пикселях экрана")]
        [SerializeField] private Vector4 _extraPadding;

        private RectTransform _rect;
        private Canvas _canvas;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // Поворот приходит не на всех платформах: проверка дешёвая, сравниваем два числа
            if (Screen.safeArea == _appliedSafeArea &&
                Screen.width == _appliedScreen.x &&
                Screen.height == _appliedScreen.y) return;

            Apply();
        }

        private void Apply()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safeArea = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screen.x;
            anchorMin.y /= screen.y;
            anchorMax.x /= screen.x;
            anchorMax.y /= screen.y;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;

            // offsetMin/offsetMax задаются в единицах Canvas, а не в пикселях экрана:
            // пиксели делим на scaleFactor, иначе отступ получается почти нулевым
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

            _rect.offsetMin = new Vector2(_extraPadding.x / scale, _extraPadding.y / scale);
            _rect.offsetMax = new Vector2(-_extraPadding.z / scale, -_extraPadding.w / scale);

            _appliedSafeArea = safeArea;
            _appliedScreen = screen;
        }
    }
}
