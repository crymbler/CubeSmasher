using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Вертикальный градиент на Image: темнее сверху, светлее к низу.
    /// Текстура 1xN рисуется один раз в Awake. Спрайт назначать не нужно.
    /// Вешается на полноэкранный Image (объект Background на сцене загрузки).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GradientBackground : MonoBehaviour
    {
        [SerializeField] private Color _top = new Color(0.56f, 0.83f, 1f);
        [SerializeField] private Color _bottom = new Color(0.91f, 0.96f, 1f);

        [Tooltip("Высота текстуры в пикселях: больше — плавнее")]
        [SerializeField, Range(2, 512)] private int _height = 128;

        private void Awake()
        {
            Apply();
        }

        private void Apply()
        {
            var image = GetComponent<Image>();
            if (image == null) return;

            var texture = new Texture2D(1, _height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "GradientBackground"
            };

            for (int y = 0; y < _height; y++)
            {
                float t = _height <= 1 ? 0f : (float)y / (_height - 1);
                texture.SetPixel(0, y, Color.Lerp(_bottom, _top, t));
            }
            texture.Apply();

            image.sprite = Sprite.Create(texture, new Rect(0, 0, 1, _height), new Vector2(0.5f, 0.5f), 100f);
            image.type = Image.Type.Simple;
        }
    }
}
