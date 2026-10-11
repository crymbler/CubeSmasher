using System;
using CubeSmasher.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Окно гаража. Показывает выбранную машину: её модель (ModelPrefab) в точке
/// _showcase, название, описание, доход и статус (открыта или закрыта по стадии).
/// Не знает о сервисах: получает данные через IGarageView.
/// </summary>
public class GarageView : MonoBehaviour, IGarageView
{
    public event Action OnOpened;
    public event Action OnClosed;
    public event Action<int> CarSelected;

    [Header("Окно")]
    [SerializeField] private GameObject _window;
    [SerializeField] private Button _closeButton;

    [Header("Камеры")]
    [Tooltip("Камера арены (миска с деталями). Если пусто, берётся Camera.main")]
    [SerializeField] private Camera _arenaCamera;

    [Tooltip("Камера гаража, смотрит на витрину с машиной. Если пусто, камеры не переключаются")]
    [SerializeField] private Camera _garageCamera;

    [Header("Витрина")]
    [Tooltip("Пустой объект, куда будет инстанцирована модель выбранной машины")]
    [SerializeField] private Transform _showcase;

    [Header("Текст")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _incomeText;
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("Выбор")]
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;

    private GameObject _currentModel;
    private int _index;
    private int _count;

    private void Awake()
    {
        if (_arenaCamera == null) _arenaCamera = Camera.main;
        if (_garageCamera != null) _garageCamera.enabled = false;

        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_prevButton != null) _prevButton.onClick.AddListener(() => CarSelected?.Invoke(Mathf.Max(0, _index - 1)));
        if (_nextButton != null) _nextButton.onClick.AddListener(() => CarSelected?.Invoke(Mathf.Min(_count - 1, _index + 1)));
    }

    public void Open()
    {
        _window.SetActive(true);
        SwitchCamera(toGarage: true);
        OnOpened?.Invoke();
    }

    public void Close()
    {
        _window.SetActive(false);
        SwitchCamera(toGarage: false);
        OnClosed?.Invoke();
    }

    /// <summary>Переключает вид: гараж показывает витрину, закрытие возвращает арену.</summary>
    private void SwitchCamera(bool toGarage)
    {
        if (_garageCamera == null) return;

        if (_arenaCamera != null) _arenaCamera.enabled = !toGarage;
        _garageCamera.enabled = toGarage;
    }

    /// <summary>Презентер сообщает, сколько машин всего, чтобы кнопки знали границы.</summary>
    public void SetCarCount(int count)
    {
        _count = count;
    }

    public void ShowCar(int index, string name, string description, double incomePerSecond,
                        bool isUnlocked, int unlockStage, GameObject modelPrefab)
    {
        _index = index;

        if (_nameText != null) _nameText.text = name;
        if (_descriptionText != null) _descriptionText.text = description;
        if (_incomeText != null)
            _incomeText.text = isUnlocked ? Localization.Get("gar.income", incomePerSecond.ToString("N0")) : Localization.Get("gar.income.none");
        if (_statusText != null)
            _statusText.text = isUnlocked ? Localization.Get("gar.status.open") : Localization.Get("gar.status.locked", unlockStage);

        ReplaceModel(isUnlocked ? modelPrefab : null);
    }

    private void ReplaceModel(GameObject prefab)
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }

        if (prefab != null && _showcase != null)
        {
            _currentModel = Instantiate(prefab, _showcase);
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        if (_prevButton != null) _prevButton.onClick.RemoveAllListeners();
        if (_nextButton != null) _nextButton.onClick.RemoveAllListeners();
    }
}
