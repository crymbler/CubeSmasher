using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Анимированный прогресс-бар: значение плавно доезжает до цели, а сам бар
/// на время наполнения чуть увеличивается и светлеет.
///
/// Работает с двумя видами баров, которые есть в проекте:
/// - Slider (верхний HUD, прогресс стадии);
/// - Fill-объект, растянутый через anchorMax.x (полоска уровня в карточке магазина).
///
/// Время нереальное (unscaledDeltaTime): магазин и гараж открываются с паузой
/// (Time.timeScale = 0), и на обычном времени анимация бы стояла.
///
/// Компонент можно не добавлять руками: AnimatedProgressBar.For(...) найдёт
/// или повесит его кодом.
/// </summary>
public class AnimatedProgressBar : MonoBehaviour
{
    [Tooltip("Slider, если бар сделан слайдером. Пусто — ищется на этом объекте.")]
    [SerializeField] private Slider _slider;

    [Tooltip("Заливка, которая растягивается через anchorMax.x. Используется, если нет Slider.")]
    [SerializeField] private RectTransform _fill;

    [Tooltip("Картинка, которая светлеет при наполнении. Пусто — Image заливки.")]
    [SerializeField] private Graphic _glow;

    [Header("Блик и частицы")]
    [Tooltip("Блик на кромке заливки (объект Shine из ProgressBar.prefab). Пусто — ищется по имени")]
    [SerializeField] private RectTransform _shine;

    [Tooltip("Частицы у кромки заливки во время роста. Выключается, если не нужно")]
    [SerializeField] private bool _emitSparks = true;

    [Tooltip("Частиц за один шаг роста (на каждый заметный прирост значения)")]
    [SerializeField, Range(1, 20)] private int _sparksPerStep = 6;

    [Header("Анимация")]
    [SerializeField, Min(0.01f)] private float _fillDuration = 0.45f;
    [Tooltip("Насколько бар увеличивается в пике наполнения (0.06 = на 6%)")]
    [SerializeField, Range(0f, 0.3f)] private float _punchScale = 0.06f;
    [Tooltip("Насколько светлеет заливка в пике (0..1, доля смешивания с белым)")]
    [SerializeField, Range(0f, 1f)] private float _flashStrength = 0.45f;

    private float _value;
    private bool _hasValue;
    private Color _glowBase;
    private bool _glowBaseCaptured;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _routine;
    private ParticleSystem _sparks;
    private float _sparkAccumulated;

    /// <summary>Найти или добавить компонент на объект бара.</summary>
    public static AnimatedProgressBar For(Component barRoot)
    {
        if (barRoot == null) return null;
        var bar = barRoot.GetComponent<AnimatedProgressBar>();
        if (bar == null) bar = barRoot.gameObject.AddComponent<AnimatedProgressBar>();
        if (barRoot is Slider slider && bar._slider == null) bar._slider = slider;
        return bar;
    }

    /// <summary>Для баров через anchorMax.x: указать заливку кодом.</summary>
    public AnimatedProgressBar WithFill(RectTransform fill)
    {
        if (fill != null) _fill = fill;
        return this;
    }

    private bool _initialized;

    private void Awake() => Init();

    // Awake не вызывается, если компонент добавили на выключенный объект,
    // поэтому инициализация ещё и ленивая — из SetValue
    private void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (_slider == null) _slider = GetComponent<Slider>();
        _baseScale = transform.localScale;

        if (_shine == null)
        {
            Transform found = FindDeep(transform, "Shine");
            if (found != null) _shine = found as RectTransform;
        }
        if (_shine != null) _shine.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        // Окно закрыли посреди анимации: доводим до цели, чтобы не застрять на полпути
        if (_routine != null)
        {
            _routine = null;
            Apply(_value);
            ResetFx();
        }
    }

    /// <summary>
    /// Задать значение 0..1. Рост анимируется; уменьшение (новая стадия, сброс)
    /// ставится сразу — "откат" полоски выглядел бы как ошибка.
    /// </summary>
    public void SetValue(float target, bool instant = false)
    {
        Init();
        target = Mathf.Clamp01(target);
        CaptureGlowBase();

        bool canAnimate = isActiveAndEnabled && _hasValue && !instant && target > _value + 0.0001f;

        if (!canAnimate)
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _value = target;
            _hasValue = true;
            Apply(target);
            ResetFx();
            return;
        }

        float from = CurrentShown();
        _value = target;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Animate(from, target));
    }

    private IEnumerator Animate(float from, float to)
    {
        _lastEdge = -1f;
        _sparkAccumulated = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / _fillDuration;
            float k = Mathf.Clamp01(t);
            float eased = 1f - Mathf.Pow(1f - k, 3f); // быстрый старт, мягкая остановка

            float current = Mathf.Lerp(from, to, eased);
            Apply(current);
            UpdateEdgeFx(current, from, to, k);

            // Пик эффекта в середине наполнения, к концу возвращается к норме
            float pulse = Mathf.Sin(k * Mathf.PI);
            transform.localScale = _baseScale * (1f + _punchScale * pulse);
            if (_glow != null) _glow.color = Color.Lerp(_glowBase, Color.white, _flashStrength * pulse);

            yield return null;
        }

        Apply(to);
        ResetFx();
        HideEdgeFx();
        _routine = null;
    }

    /// <summary>
    /// Блик и искры на кромке заливки. Кромка — это точка anchorMax.x у заливки
    /// (или конец Slider.fillRect). Блик показывается, пока заливка растёт,
    /// искры вылетают при каждом заметном приросте, а не каждый кадр.
    /// </summary>
    private void UpdateEdgeFx(float current, float from, float to, float k)
    {
        RectTransform rt = EdgeRect();
        if (rt == null) return;

        if (_shine != null)
        {
            _shine.gameObject.SetActive(true);
            // Блик — тонкая полоска шириной 8 px, центр которой стоит на кромке заливки.
            // При нулевой ширине (anchorMin.x == anchorMax.x и sizeDelta.x == 0) он невидим.
            _shine.anchorMin = new Vector2(current, _shine.anchorMin.y);
            _shine.anchorMax = new Vector2(current, _shine.anchorMax.y);
            _shine.pivot = new Vector2(0.5f, _shine.pivot.y);
            _shine.sizeDelta = new Vector2(8f, _shine.sizeDelta.y);
        }

        if (!_emitSparks || to <= from) return;

        // Искры копятся: вылетают пачкой каждые 2% прироста, а не на каждый кадр
        _sparkAccumulated += (current - (_lastEdge < 0 ? from : _lastEdge));
        _lastEdge = current;
        if (_sparkAccumulated < 0.02f) return;
        _sparkAccumulated = 0f;

        EnsureSparks();
        if (_sparks == null) return;

        // Позиция кромки на экране: по локальным координатам RectTransform заливки
        Rect r = rt.rect;
        float x = Mathf.Lerp(r.xMin, r.xMax, current);
        _sparks.transform.localPosition = new Vector3(x, 0f, 0f);
        _sparks.Emit(_sparksPerStep);
    }

    private float _lastEdge = -1f;

    private RectTransform EdgeRect()
    {
        if (_fill != null) return _fill.parent as RectTransform;
        if (_slider != null && _slider.fillRect != null) return _slider.fillRect.parent as RectTransform;
        return null;
    }

    private void HideEdgeFx()
    {
        if (_shine != null) _shine.gameObject.SetActive(false);
        _sparkAccumulated = 0f;
        _lastEdge = -1f;
    }

    /// <summary>
    /// Создаёт систему частиц в коде: короткая вспышка неоново-зелёных искр
    /// в цвете UITheme.NeonGreen. Префаб не нужен.
    /// </summary>
    private void EnsureSparks()
    {
        if (_sparks != null) return;

        RectTransform rt = EdgeRect();
        if (rt == null) return;

        var go = new GameObject("EdgeSparks", typeof(RectTransform));
        go.transform.SetParent(rt, false);
        go.transform.localPosition = Vector3.zero;

        _sparks = go.AddComponent<ParticleSystem>();
        var main = _sparks.main;
        main.loop = false;
        main.playOnAwake = false;
        main.useUnscaledTime = true;   // работает при паузе, как и анимация бара
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 60f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startColor = CubeSmasher.UI.UITheme.NeonGreen;
        main.gravityModifier = 0.3f;
        main.maxParticles = 60;

        var emission = _sparks.emission;
        emission.enabled = false;   // только по Emit(), без постоянного потока

        var shape = _sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = UnlockEffect.MakeMaterial(CubeSmasher.UI.UITheme.NeonGreen);
        renderer.sortingOrder = 10;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    private float CurrentShown()
    {
        if (_slider != null)
        {
            float range = _slider.maxValue - _slider.minValue;
            return range > 0f ? (_slider.value - _slider.minValue) / range : 0f;
        }
        if (_fill != null) return _fill.anchorMax.x;
        return _value;
    }

    private void Apply(float v)
    {
        if (_slider != null)
        {
            _slider.value = Mathf.Lerp(_slider.minValue, _slider.maxValue, v);
            return;
        }
        if (_fill != null)
        {
            Vector2 max = _fill.anchorMax;
            max.x = v;
            _fill.anchorMax = max;
        }
    }

    private void CaptureGlowBase()
    {
        if (_glowBaseCaptured) return;
        if (_glow == null)
        {
            if (_fill != null) _glow = _fill.GetComponent<Graphic>();
            else if (_slider != null && _slider.fillRect != null) _glow = _slider.fillRect.GetComponent<Graphic>();
        }
        if (_glow != null) _glowBase = _glow.color;
        _glowBaseCaptured = true;
    }

    private void ResetFx()
    {
        transform.localScale = _baseScale;
        if (_glow != null && _glowBaseCaptured) _glow.color = _glowBase;
    }
}
