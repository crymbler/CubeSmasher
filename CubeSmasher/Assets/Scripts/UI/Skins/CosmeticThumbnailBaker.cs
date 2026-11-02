using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Превращает 3D-модель в спрайт для карточки магазина.
    ///
    /// Зачем: в слоте нужно видеть сам молот, а не цветной прямоугольник.
    /// UI не умеет показывать 3D-объекты напрямую, поэтому модель рендерится
    /// один раз в текстуру и дальше показывается как обычная картинка.
    ///
    /// Как это работает:
    /// 1. Модель создаётся далеко за пределами сцены (z = -20000), чтобы её
    ///    не увидела игровая камера и она не попала в кадр.
    /// 2. К ней подставляется временная камера, которая кадрирует объект по его
    ///    границам, и две временные лампы, иначе воксели получаются чёрными.
    /// 3. Камера рисует один кадр в RenderTexture, оттуда пиксели читаются
    ///    в Texture2D и превращаются в Sprite.
    /// 4. Временные объекты удаляются, спрайт кладётся в кэш.
    ///
    /// Слой не переключается намеренно: `go.layer = -1` бросает исключение,
    /// если нужного слоя нет в проекте. Дальнее смещение решает ту же задачу
    /// без риска.
    ///
    /// Кэш общий на всё приложение: каждый предмет рендерится один раз,
    /// сколько бы раз игрок ни открывал магазин.
    /// </summary>
    public static class CosmeticThumbnailBaker
    {
        private const int Size = 256;
        private const float Fov = 28f;
        private const float DistanceFromScene = 20000f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Номер текущей съёмки. Каждая модель ставится в свою точку, иначе
        /// при нескольких выпечках подряд они оказываются в одном месте
        /// и попадают в кадр друг друга.
        /// </summary>
        private static int _bakeIndex;

        /// <summary>
        /// Спрайт предмета. Ключ кэша — модель плюс материал: одна и та же модель
        /// с разными материалами (тарелки) даёт разные картинки.
        /// </summary>
        public static Sprite Get(string cacheKey, GameObject modelPrefab, Material material)
        {
            if (string.IsNullOrEmpty(cacheKey)) return null;
            if (Cache.TryGetValue(cacheKey, out Sprite cached)) return cached;

            Sprite baked = Bake(modelPrefab, material);
            Cache[cacheKey] = baked;
            return baked;
        }

        public static void ClearCache()
        {
            foreach (KeyValuePair<string, Sprite> pair in Cache)
            {
                if (pair.Value == null) continue;

                if (pair.Value.texture != null) Object.Destroy(pair.Value.texture);
                Object.Destroy(pair.Value);
            }
            Cache.Clear();
        }

        private static Sprite Bake(GameObject modelPrefab, Material material)
        {
            if (modelPrefab == null) return null;

            // Своя точка на каждую модель: даже если предыдущая ещё жива
            // до конца кадра, она не попадёт в кадр этой камеры
            _bakeIndex++;
            Vector3 origin = new Vector3(0f, -DistanceFromScene - _bakeIndex * 100f, 0f);

            GameObject instance = Object.Instantiate(modelPrefab, origin, Quaternion.identity);
            instance.name = "ThumbnailSource";

            // Материал подставляем до расчёта границ: смена материала
            // не меняет габариты, но порядок так понятнее
            if (material != null)
            {
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }
            }

            if (!TryGetBounds(instance, out Bounds bounds))
            {
                Object.Destroy(instance);
                return null;
            }

            GameObject rig = new GameObject("ThumbnailRig");
            rig.transform.position = origin;

            Camera camera = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Sprite sprite = null;

            try
            {
                camera = CreateCamera(rig.transform, bounds, origin);
                CreateLights(rig.transform);

                renderTexture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4
                };

                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;

                texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
                texture.Apply();

                RenderTexture.active = previous;

                sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size),
                                       new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = "Thumbnail";
            }
            finally
            {
                // Object.Destroy откладывается до конца кадра. Пока объекты живы,
                // следующая выпечка видит их в кадре и получает наложенные модели.
                // Поэтому гасим немедленно, а удаляем как обычно.
                if (rig != null) rig.SetActive(false);
                if (instance != null) instance.SetActive(false);

                if (camera != null)
                {
                    camera.targetTexture = null;
                    camera.enabled = false;
                    Object.Destroy(camera.gameObject);
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.Destroy(renderTexture);
                }

                Object.Destroy(rig);
                Object.Destroy(instance);
            }

            return sprite;
        }

        private static Camera CreateCamera(Transform parent, Bounds bounds, Vector3 origin)
        {
            var go = new GameObject("ThumbnailCamera");
            go.transform.SetParent(parent, false);

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Прозрачный фон: карточка магазина сама задаёт цвет, модель
            // должна ложиться на него без чёрного квадрата
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = Fov;
            camera.nearClipPlane = 0.01f;

            // Наклон сверху-сбоку: так у молота видно и ручку, и боёк
            Vector3 direction = new Vector3(0.55f, 0.45f, -1f).normalized;
            float radius = Mathf.Max(0.01f, bounds.extents.magnitude);
            float distance = radius / Mathf.Sin(Fov * 0.5f * Mathf.Deg2Rad) * 1.15f;

            // Дальняя плоскость строго вокруг модели. Раньше здесь стояло
            // DistanceFromScene * 2f = 40000, и камера добивала до самой игровой
            // сцены: в картинку попадали настоящие молоты со SwingPivot.
            camera.farClipPlane = distance * 2f + radius * 2f;

            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.enabled = false; // рисуем вручную через Render()

            // Только слой по умолчанию: игровые объекты UI и эффектов
            // не должны попадать в кадр бейкера
            camera.cullingMask = 1 << 0;

            Vector3 lookAt = bounds.center;
            go.transform.position = lookAt - direction * distance;
            go.transform.LookAt(lookAt);

            return camera;
        }

        private static void CreateLights(Transform parent)
        {
            // Ключевой свет
            var keyGo = new GameObject("ThumbnailKeyLight");
            keyGo.transform.SetParent(parent, false);
            keyGo.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.35f;
            key.color = Color.white;

            // Заполняющий свет с другой стороны, чтобы тень не была чёрной
            var fillGo = new GameObject("ThumbnailFillLight");
            fillGo.transform.SetParent(parent, false);
            fillGo.transform.rotation = Quaternion.Euler(15f, 150f, 0f);

            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.75f, 0.85f, 1f);
        }

        /// <summary>Общие границы всех рендереров модели.</summary>
        private static bool TryGetBounds(GameObject instance, out Bounds bounds)
        {
            bounds = default;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;

            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }
    }
}
