using UnityEngine;
using TMPro;
using YG;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LanguageSwitcher : MonoBehaviour
{
    [TextArea(1, 100)]
    [SerializeField] private string _ru;
    [TextArea(1, 100)]
    [SerializeField] private string _en;

    private TextMeshProUGUI _textComponent;

    private void Awake()
    {
        // Кэшируем компонент один раз при старте
        _textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Подписываемся на событие смены языка при включении объекта
        YG2.onSwitchLang += SwitchLanguage;
        
        // Принудительно обновляем текст при каждой активации окна (SetActive(true)).
        // YG2.lang хранит текущий выбранный язык в плагине PluginYG.
        SwitchLanguage(YG2.lang);
    }

    private void OnDisable()
    {
        // Обязательно отписываемся при выключении, чтобы избежать ошибок и утечек памяти
        YG2.onSwitchLang -= SwitchLanguage;
    }

    public void SwitchLanguage(string lang)
    {
        // Скрипт только меняет текст в зависимости от переданного ключа
        switch (lang)
        {
            case "ru":
                _textComponent.text = _ru;
                break;
            default:
                _textComponent.text = _en;
                break;
        }
    }
}