using System;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран гаража. Показывает машину в одном из трёх состояний (см. GarageCarState):
/// закрыта — плашка "Откроется на стадии N"; доступна — модель видна, но без описания
/// и характеристик, есть кнопка "Открыть"; открыта — видно описание и характеристики.
/// Открытие выполняет GarageService, здесь только запрос и эффект.
/// </summary>
public class GarageView : MonoBehaviour, IGarageView
{
    public event Action OnOpened;
    public event Action OnClosed;
    public event Action<int> CarSelected;
    public event Action<int> CarOpenRequested;

    [Header("Экран")]
    [SerializeField] private GameObject _window;
    [SerializeField] private Button _closeButton;
    [Tooltip("Всё, что нужно спрятать на время гаража: HUD, нижняя панель с кнопками")]
    [SerializeField] private GameObject[] _hideWhileOpen;

    [Header("Камеры")]
    [SerializeField] private Camera _arenaCamera;
    [SerializeField] private Camera _garageCamera;

    [Header("Витрина")]
    [Tooltip("Пустой объект, куда ставится модель выбранной машины")]
    [SerializeField] private Transform _showcase;

    [Header("Тексты открытой машины")]
    [SerializeField] private GameObject _infoRoot;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _incomeText;
    [SerializeField] private TextMeshProUGUI _statsText;

    [Header("Закрытая машина")]
    [SerializeField] private GameObject _lockPlate;
    [SerializeField] private TextMeshProUGUI _lockText;

    [Header("Кнопка открытия")]
    [SerializeField] private GameObject _openButtonRoot;
    [SerializeField] private Button _openButton;
    [SerializeField] private TextMeshProUGUI _openButtonText;

    [Header("Листание")]
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [Tooltip("Иконки машин внизу. Создаются из префаба при SetCarCount")]
    [SerializeField] private Transform _thumbnailsContainer;
    [SerializeField] private Button _thumbnailPrefab;

    [Header("Раскладка")]
    [Tooltip("Нижняя полоса. Вью сам подгоняет размеры иконок и стрелок под ориентацию экрана")]
    [SerializeField] private RectTransform _bottomBar;
    [Tooltip("Прокрутка ленты: при большом числе машин лента едет, а не сжимается в кучу")]
    [SerializeField] private ScrollRect _thumbnailsScroll;

    [Header("Эффект открытия")]
    [SerializeField] private ParticleSystem _unlockParticles;
    [SerializeField] private ParticleSystem _unlockParticlesSecondary;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _unlockSound;

    // Размеры подобраны так, чтобы на телефоне иконки не липли друг к другу,
    // а на ПК лента не растягивалась на пол страницы.
    private const float ThumbLandscape = 132f;
    private const float ThumbPortrait = 104f;
    private const float GapLandscape = 20f;
    private const float GapPortrait = 14f;
    private const float ArrowLandscape = 110f;
    private const float ArrowPortrait = 84f;
    private const float ArrowGap = 24f;

    private float _thumbSize = ThumbLandscape;

    private int _lastWidth = -1;
    private int _lastHeight = -1;

    private GameObject _currentModel;
    private readonly System.Collections.Generic.List<Button> _thumbnails = new System.Collections.Generic.List<Button>();

    // Иконка и кружок каждой миниатюры: приглушаем закрытые, зажигаем доступные.
    private readonly System.Collections.Generic.List<Image> _thumbnailIcons = new System.Collections.Generic.List<Image>();
    private readonly System.Collections.Generic.List<GameObject> _thumbnailDots = new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<GarageCarState> _thumbnailStates = new System.Collections.Generic.List<GarageCarState>();

    private const float LockedIconAlpha = 0.35f;
    private int _index;
    private int _count;

    private void Awake()
    {
        if (_arenaCamera == null) _arenaCamera = Camera.main;
        if (_garageCamera != null) _garageCamera.enabled = false;

        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_prevButton != null) _prevButton.onClick.AddListener(Prev);
        if (_nextButton != null) _nextButton.onClick.AddListener(Next);
        if (_openButton != null) _openButton.onClick.AddListener(RequestOpen);

        ApplyResponsiveLayout();
    }

    /// <summary>Экран повернули или изменили размер окна — пересчитываем размеры.</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
        ApplyResponsiveLayout();
    }

    /// <summary>
    /// Портрет (телефон) — иконки и стрелки мельче, лента прокручивается по центру.
    /// Альбом (ПК) — крупнее. Логика одна, меняются только числа.
    /// </summary>
    private void ApplyResponsiveLayout()
    {
        if (_thumbnailsContainer == null) return;

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        bool portrait = Screen.height > Screen.width;

        _thumbSize = portrait ? ThumbPortrait : ThumbLandscape;
        float gap = portrait ? GapPortrait : GapLandscape;
        float arrow = portrait ? ArrowPortrait : ArrowLandscape;

        if (_thumbnailsScroll != null && _thumbnailsScroll.content != null)
        {
            var layout = _thumbnailsScroll.content.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.spacing = gap;
        }

        SetArrowSize(_prevButton, arrow);
        SetArrowSize(_nextButton, arrow);

        // Лента занимает всё между стрелками, поэтому отступ зависит от их размера
        if (_thumbnailsScroll != null && _thumbnailsScroll.viewport != null)
        {
            RectTransform vp = _thumbnailsScroll.viewport;
            float side = 32f + arrow + ArrowGap;
            vp.offsetMin = new Vector2(side, vp.offsetMin.y);
            vp.offsetMax = new Vector2(-side, vp.offsetMax.y);
        }

        for (int i = 0; i < _thumbnails.Count; i++)
        {
            if (_thumbnails[i] == null) continue;
            var le = _thumbnails[i].GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = _thumbSize;
                le.preferredHeight = _thumbSize;
            }
        }

        ScrollToSelected();
    }

    private static void SetArrowSize(Button button, float size)
    {
        if (button == null) return;
        var rt = button.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(size, size);
    }

    /// <summary>Подвести ленту к выбранной машине, чтобы она всегда была на виду.</summary>
    private void ScrollToSelected()
    {
        if (_thumbnailsScroll == null || _thumbnailsScroll.content == null) return;
        if (_thumbnailsScroll.viewport == null) return;

        Canvas.ForceUpdateCanvases();

        float viewportWidth = _thumbnailsScroll.viewport.rect.width;
        float contentWidth = _thumbnailsScroll.content.rect.width;
        if (contentWidth <= viewportWidth + 1f)
        {
            _thumbnailsScroll.horizontalNormalizedPosition = 0f;
            return;
        }

        float cell = _thumbSize + 20f; // иконка + отступ сетки
        float target = _index * cell + _thumbSize * 0.5f - viewportWidth * 0.5f;
        _thumbnailsScroll.horizontalNormalizedPosition =
            Mathf.Clamp01(target / (contentWidth - viewportWidth));
    }

    public void Open()
    {
        _window.SetActive(true);
        SetHidden(true);
        SwitchCamera(toGarage: true);
        OnOpened?.Invoke();
    }

    public void Close()
    {
        _window.SetActive(false);
        SetHidden(false);
        SwitchCamera(toGarage: false);
        OnClosed?.Invoke();
    }

    /// <summary>Прячет HUD и нижнюю панель, пока открыт гараж, и возвращает как было.</summary>
    private void SetHidden(bool hide)
    {
        if (_hideWhileOpen == null) return;

        foreach (GameObject go in _hideWhileOpen)
        {
            if (go != null) go.SetActive(!hide);
        }
    }

    private void SwitchCamera(bool toGarage)
    {
        if (_garageCamera == null) return;
        if (_arenaCamera != null) _arenaCamera.enabled = !toGarage;
        _garageCamera.enabled = toGarage;
    }

    private void Prev() => CarSelected?.Invoke(Mathf.Max(0, _index - 1));

    private void Next() => CarSelected?.Invoke(Mathf.Min(_count - 1, _index + 1));

    private void RequestOpen() => CarOpenRequested?.Invoke(_index);

    public void SetCarCount(Sprite[] icons, GarageCarState[] states)
    {
        _count = icons != null ? icons.Length : 0;
        BuildThumbnails(icons, states);
    }

    /// <summary>Закрытая иконка приглушается, у доступной горит кружок.</summary>
    public void SetCarState(int index, GarageCarState state)
    {
        if (index < 0 || index >= _thumbnailStates.Count) return;

        _thumbnailStates[index] = state;
        ApplyThumbnailVisual(index);
    }

    private void ApplyThumbnailVisual(int index)
    {
        if (index < 0 || index >= _thumbnailStates.Count) return;

        bool locked = _thumbnailStates[index] == GarageCarState.Locked;

        if (index < _thumbnailIcons.Count && _thumbnailIcons[index] != null)
        {
            Color c = _thumbnailIcons[index].color;
            c.a = locked ? LockedIconAlpha : 1f;
            _thumbnailIcons[index].color = c;
        }

        // Кружок только у доступной: открытая уже не требует внимания
        if (index < _thumbnailDots.Count && _thumbnailDots[index] != null)
            _thumbnailDots[index].SetActive(_thumbnailStates[index] == GarageCarState.Available);
    }

    /// <summary>Иконки внизу: по кнопке на машину. Нажатие выбирает машину.</summary>
    private void BuildThumbnails(Sprite[] icons, GarageCarState[] states)
    {
        if (_thumbnailsContainer == null || _thumbnailPrefab == null || icons == null) return;

        foreach (Button old in _thumbnails) if (old != null) Destroy(old.gameObject);
        _thumbnails.Clear();
        _thumbnailIcons.Clear();
        _thumbnailDots.Clear();
        _thumbnailStates.Clear();

        for (int i = 0; i < icons.Length; i++)
        {
            Button btn = Instantiate(_thumbnailPrefab, _thumbnailsContainer);
            btn.gameObject.SetActive(true);

            // MiniIcon из GarageCarData. Без иконки миниатюра остаётся пустой кнопкой.
            Image icon = btn.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = icons[i];

            // Кружок "машина ждёт открытия". Создаётся кодом: префаб миниатюры
            // верстается руками, заводить в нём объект заранее не нужно.
            GameObject dot = NotificationDot.Ensure(btn.transform, "Dot", new Vector2(-2f, -2f), 24f);

            int index = i; // копия для замыкания
            btn.onClick.AddListener(() => CarSelected?.Invoke(index));

            _thumbnails.Add(btn);
            _thumbnailIcons.Add(icon);
            _thumbnailDots.Add(dot);
            _thumbnailStates.Add(states != null && i < states.Length ? states[i] : GarageCarState.Locked);

            ApplyThumbnailVisual(i);
        }

        ApplyResponsiveLayout();
    }

    public void ShowCar(int index, string name, string description, string characteristics,
                        double incomePerSecond, GarageCarState state, int unlockStage,
                        GameObject modelPrefab)
    {
        _index = index;
        bool opened = state == GarageCarState.Opened;
        bool available = state == GarageCarState.Available;

        // Название видно всегда: игрок должен понимать, что за машина
        if (_nameText != null) _nameText.text = name;

        // Описание и характеристики только у открытой
        if (_infoRoot != null) _infoRoot.SetActive(opened);
        if (_descriptionText != null) _descriptionText.text = opened ? description : string.Empty;
        if (_incomeText != null)
        {
            _incomeText.text = opened
                ? Localization.Get("gar.income", incomePerSecond.ToString("N0"))
                : string.Empty;
        }
        if (_statsText != null) _statsText.text = opened ? characteristics : string.Empty;

        // Плашка закрытой машины
        if (_lockPlate != null) _lockPlate.SetActive(state == GarageCarState.Locked);
        if (_lockText != null && state == GarageCarState.Locked)
            _lockText.text = Localization.Get("gar.status.locked", unlockStage);

        // Кнопка "Открыть" только у доступной
        if (_openButtonRoot != null) _openButtonRoot.SetActive(available);
        if (_openButtonText != null && available)
            _openButtonText.text = Localization.Get("gar.open");

        // Модель видна у доступной и открытой, у закрытой её нет
        ReplaceModel(opened || available ? modelPrefab : null);
        HighlightThumbnail(index);
        ScrollToSelected();
    }

    private void HighlightThumbnail(int index)
    {
        for (int i = 0; i < _thumbnails.Count; i++)
        {
            if (_thumbnails[i] == null) continue;
            var img = _thumbnails[i].GetComponent<Image>();
            if (img == null) continue;
            img.color = i == index
                ? CubeSmasher.UI.UITheme.NeonGreen
                : CubeSmasher.UI.UITheme.MetalMid;
        }
    }

    /// <summary>Партиклы и торжественный звук в момент открытия машины.</summary>
    public void PlayUnlockEffect()
    {
        if (_unlockSound == null) _unlockSound = UnlockEffect.CreateUnlockClip();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }
        _audioSource.PlayOneShot(_unlockSound);

        Transform anchor = _showcase != null ? _showcase : transform;

        // Два залпа: зелёный и розовый. Если поле пустое, шаблон строится кодом.
        if (_unlockParticles == null)
            _unlockParticles = UnlockEffect.CreateTemplate(anchor, "UnlockBurstGreen", UnlockEffect.NeonGreen);
        if (_unlockParticlesSecondary == null)
            _unlockParticlesSecondary = UnlockEffect.CreateTemplate(anchor, "UnlockBurstPink", UnlockEffect.NeonPink);

        SpawnBurst(_unlockParticles, anchor);
        SpawnBurst(_unlockParticlesSecondary, anchor);
    }

    private void SpawnBurst(ParticleSystem source, Transform anchor)
    {
        if (source == null) return;
        ParticleSystem instance = Instantiate(source, anchor);
        instance.transform.localPosition = Vector3.zero;
        instance.gameObject.SetActive(true);
        instance.Play(true); // удаляется сам по stopAction = Destroy
    }

    private void ReplaceModel(GameObject prefab)
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }

        if (prefab == null || _showcase == null) return;

        _currentModel = Instantiate(prefab, _showcase);
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        if (_prevButton != null) _prevButton.onClick.RemoveAllListeners();
        if (_nextButton != null) _nextButton.onClick.RemoveAllListeners();
        if (_openButton != null) _openButton.onClick.RemoveAllListeners();
    }
}
