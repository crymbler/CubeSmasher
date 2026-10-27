using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeSmasher.Core
{
    /// <summary>
    /// Экран загрузки: падающие кубы как водопад и асинхронная загрузка игровой сцены.
    ///
    /// Все размеры заданы в долях экрана, а не в мировых единицах: значения не зависят
    /// от FOV камеры, её позиции и расстояния до Canvas. Позиция куба считается через
    /// ViewportToWorldPoint на заданной глубине, поэтому область спавна всегда совпадает
    /// с видимой частью экрана при любом соотношении сторон.
    /// </summary>
    public class BootScreen : MonoBehaviour
    {
        [Header("Загрузка")]
        [Tooltip("Имя игровой сцены, которую нужно загрузить (как в Build Settings)")]
        [SerializeField] private string _gameSceneName = "CubeScene";

        [Tooltip("Минимальное время показа экрана, сек")]
        [SerializeField] private float _minShowSeconds = 1.5f;

        [Tooltip("Полоса загрузки (необязательно)")]
        [SerializeField] private Slider _progressBar;

        [Header("Кубы")]
        [Tooltip("Префаб или любой куб для водопада. Физика и игровые скрипты не нужны")]
        [SerializeField] private GameObject _fallingCubePrefab;

        [Tooltip("Сколько кубов в водопаде")]
        [SerializeField, Range(1, 120)] private int _cubeCount = 40;

        [Tooltip("Камера. Пусто — берётся Camera.main")]
        [SerializeField] private Camera _camera;

        [Tooltip("Расстояние от камеры до плоскости водопада, в мировых единицах. " +
                 "Должно быть меньше planeDistance у Canvas, иначе фон перекроет кубы")]
        [SerializeField] private float _depth = 25f;

        [Header("Область и движение (в долях экрана)")]
        [Tooltip("Ширина области спавна: 1 = вся ширина экрана")]
        [SerializeField, Range(0.1f, 1.5f)] private float _areaWidth = 1f;

        [Tooltip("Доля экрана снизу, которую не занимать: там полоса загрузки")]
        [SerializeField, Range(0f, 0.6f)] private float _bottomReserve = 0.2f;

        [Tooltip("Скорость падения в экранах за секунду. 0.55 = пролёт примерно за 1.7 с")]
        [SerializeField, Range(0.05f, 3f)] private float _fallSpeed = 0.55f;

        [Tooltip("Разброс по глубине: кубы летят в объёме, а не в одной плоскости")]
        [SerializeField] private Vector2 _depthSpread = new Vector2(-4f, 6f);

        [Header("Вид кубов")]
        [Tooltip("Размер куба в долях высоты экрана: от и до")]
        [SerializeField] private Vector2 _sizeFraction = new Vector2(0.10f, 0.20f);

        [Tooltip("Зона плавного уменьшения у нижней границы, в долях экрана")]
        [SerializeField, Range(0.01f, 0.3f)] private float _fadeZone = 0.12f;

        /// <summary>Состояние одного куба: позиция и размер в долях экрана.</summary>
        private struct FallingCube
        {
            public Transform Transform;
            public float X;
            public float Y;
            public float DepthOffset;
            public float Size;
        }

        private readonly List<FallingCube> _cubes = new List<FallingCube>();
        private float _prefabHeight = 1f;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            MeasurePrefab();
        }

        private void Start()
        {
            SpawnCubes();
            StartCoroutine(LoadRoutine());
        }

        private void Update()
        {
            if (_camera == null || _cubes.Count == 0) return;

            float dt = Time.deltaTime;
            float bottom = _bottomReserve - _fadeZone;
            float top = 1f + _fadeZone;

            for (int i = 0; i < _cubes.Count; i++)
            {
                FallingCube cube = _cubes[i];

                cube.Y -= _fallSpeed * dt;
                if (cube.Y < bottom) cube.Y = top;

                float fade = Mathf.Clamp01((cube.Y - bottom) / _fadeZone);
                if (fade <= 0f)
                {
                    cube.Transform.gameObject.SetActive(false);
                    _cubes[i] = cube;
                    continue;
                }

                cube.Transform.gameObject.SetActive(true);
                ApplyTransform(cube, fade);
                _cubes[i] = cube;
            }
        }

        /// <summary>
        /// Ставит куб в точку экрана через ViewportToWorldPoint и подбирает масштаб так,
        /// чтобы его высота равнялась заданной доле высоты экрана на этой глубине.
        /// </summary>
        private void ApplyTransform(FallingCube cube, float fade)
        {
            float depth = Mathf.Max(1f, _depth + cube.DepthOffset);

            Vector3 world = _camera.ViewportToWorldPoint(new Vector3(cube.X, cube.Y, depth));
            cube.Transform.position = world;

            float worldHeight = 2f * depth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float scale = worldHeight * cube.Size / _prefabHeight;

            cube.Transform.localScale = Vector3.one * scale * fade;
        }

        /// <summary>Высота куба-префаба: нужна, чтобы перевести долю экрана в localScale.</summary>
        private void MeasurePrefab()
        {
            if (_fallingCubePrefab == null) return;

            MeshFilter filter = _fallingCubePrefab.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            _prefabHeight = filter.sharedMesh.bounds.size.y * _fallingCubePrefab.transform.localScale.y;
            if (_prefabHeight <= 0.0001f) _prefabHeight = 1f;
        }

        private void SpawnCubes()
        {
            if (_fallingCubePrefab == null)
            {
                Debug.LogWarning("[BootScreen] Не назначен префаб куба: водопада не будет.");
                return;
            }

            for (int i = 0; i < _cubeCount; i++)
            {
                GameObject instance = Instantiate(_fallingCubePrefab, transform);
                PrepareCube(instance);

                float halfWidth = Mathf.Clamp01(_areaWidth) * 0.5f;

                _cubes.Add(new FallingCube
                {
                    Transform = instance.transform,
                    X = Random.Range(0.5f - halfWidth, 0.5f + halfWidth),
                    Y = Random.Range(_bottomReserve, 1f + _fadeZone),
                    DepthOffset = Random.Range(_depthSpread.x, _depthSpread.y),
                    Size = Random.Range(_sizeFraction.x, _sizeFraction.y)
                });
            }
        }

        /// <summary>
        /// Гасит физику у декоративного куба и просит цвет у ColorChanger.
        /// Скрипты не снимаются: CarPart требует ColorChanger через RequireComponent,
        /// удалить один без другого нельзя.
        /// </summary>
        private void PrepareCube(GameObject cube)
        {
            foreach (Rigidbody body in cube.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            foreach (Collider col in cube.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            // Цвет уже умеет ColorChanger с префаба детали: HSV, насыщенность 0.8–1,
            // яркость 0.85–1. Он же клонирует материал, поэтому кубы выходят разного цвета.
            foreach (ColorChanger changer in cube.GetComponentsInChildren<ColorChanger>(true))
            {
                changer.ApplyAcidColor();
            }
        }

        private IEnumerator LoadRoutine()
        {
            float started = Time.realtimeSinceStartup;

            AsyncOperation op = SceneManager.LoadSceneAsync(_gameSceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                if (_progressBar != null) _progressBar.value = op.progress;
                yield return null;
            }

            while (Time.realtimeSinceStartup - started < _minShowSeconds)
            {
                if (_progressBar != null) _progressBar.value = 0.95f;
                yield return null;
            }

            if (_progressBar != null) _progressBar.value = 1f;
            op.allowSceneActivation = true;
        }
    }
}
