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

    [Header("Эффект открытия")]
    [SerializeField] private ParticleSystem _unlockParticles;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _unlockSound;

    private GameObject _currentModel;
    private readonly System.Collections.Generic.List<Button> _thumbnails = new System.Collections.Generic.List<Button>();
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

    public void SetCarCount(int count)
    {
        _count = count;
        BuildThumbnails(count);
    }

    /// <summary>Иконки внизу: по кнопке на машину. Нажатие выбирает машину.</summary>
    private void BuildThumbnails(int count)
    {
        if (_thumbnailsContainer == null || _thumbnailPrefab == null) return;

        foreach (Button old in _thumbnails) if (old != null) Destroy(old.gameObject);
        _thumbnails.Clear();

        for (int i = 0; i < count; i++)
        {
            Button btn = Instantiate(_thumbnailPrefab, _thumbnailsContainer);
            btn.gameObject.SetActive(true);

            int index = i; // копия для замыкания
            btn.onClick.AddListener(() => CarSelected?.Invoke(index));
            _thumbnails.Add(btn);
        }
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
        if (_unlockParticles != null) _unlockParticles.Play();
        if (_audioSource != null && _unlockSound != null) _audioSource.PlayOneShot(_unlockSound);
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
