using System; // ќб€зательно дл€ Action
using UnityEngine;
using UnityEngine.UI;

public class GarageView : MonoBehaviour
{
    // —обыти€, чтобы другие скрипты знали о состо€нии окна
    public event Action OnOpened;
    public event Action OnClosed;

    [Header("UI Ёлементы")]
    [SerializeField] private GameObject _window;
    [SerializeField] private Button _closeButton;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }
    }

    public void Open()
    {
        _window.SetActive(true);
        OnOpened?.Invoke(); // —ообщаем, что открылись
    }

    public void Close()
    {
        _window.SetActive(false);
        OnClosed?.Invoke(); // —ообщаем, что закрылись
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }
    }
}