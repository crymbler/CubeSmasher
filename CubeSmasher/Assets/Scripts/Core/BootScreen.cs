using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CubeSmasher.Core
{
    /// <summary>
    /// Экран загрузки: падающие кубы как «водопад» за камерой и асинхронная загрузка игровой сцены.
    /// Игрок видит движение, пока грузится CubeScene. Кубы без физики: только transform.
    /// </summary>
    public class BootScreen : MonoBehaviour
    {
        [Tooltip("Имя игровой сцены, которую нужно загрузить (как в Build Settings)")]
        [SerializeField] private string _gameSceneName = "CubeScene";

        [Tooltip("Префаб или любой куб для водопада (без Rigidbody)")]
        [SerializeField] private GameObject _fallingCubePrefab;

        [Tooltip("Сколько кубов в водопаде (30–50 по ROADMAP)")]
        [SerializeField, Range(10, 80)] private int _cubeCount = 40;

        [Tooltip("Скорость падения, единиц в секунду")]
        [SerializeField] private float _fallSpeed = 3f;

        [Tooltip("Ширина и высота области падения")]
        [SerializeField] private Vector2 _area = new Vector2(8f, 12f);

        [Tooltip("Полоса загрузки (необязательно)")]
        [SerializeField] private Slider _progressBar;

        [Tooltip("Минимальное время показа экрана, сек. Чтобы не мигал на быстрых устройствах")]
        [SerializeField] private float _minShowSeconds = 1.5f;

        private readonly List<Transform> _cubes = new List<Transform>();

        private void Start()
        {
            SpawnCubes();
            StartCoroutine(LoadRoutine());
        }

        private void Update()
        {
            // Падение и возврат наверх: без физики, без аллокаций в Update
            float bottom = -_area.y * 0.5f;
            float top = _area.y * 0.5f;
            for (int i = 0; i < _cubes.Count; i++)
            {
                Transform c = _cubes[i];
                Vector3 p = c.position;
                p.y -= _fallSpeed * Time.deltaTime;
                if (p.y < bottom) p.y = top;
                c.position = p;
            }
        }

        private void SpawnCubes()
        {
            if (_fallingCubePrefab == null) return;

            for (int i = 0; i < _cubeCount; i++)
            {
                GameObject c = Instantiate(_fallingCubePrefab, transform);
                float x = Random.Range(-_area.x * 0.5f, _area.x * 0.5f);
                float y = Random.Range(-_area.y * 0.5f, _area.y * 0.5f);
                c.transform.localPosition = new Vector3(x, y, 0f);
                c.transform.localRotation = Random.rotation;
                _cubes.Add(c.transform);
            }
        }

        private IEnumerator LoadRoutine()
        {
            float started = Time.realtimeSinceStartup;

            AsyncOperation op = SceneManager.LoadSceneAsync(_gameSceneName);
            op.allowSceneActivation = false;

            // Unity останавливает progress на 0.9 до активации сцены
            while (op.progress < 0.9f)
            {
                if (_progressBar != null) _progressBar.value = op.progress;
                yield return null;
            }

            // Не закрываем экран раньше минимального времени
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
