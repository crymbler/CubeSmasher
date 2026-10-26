using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;

/// <summary>
/// Экран выбора языка: три кнопки (ru, en, tr). Выбор сохраняет YG2 (YG2.SwitchLanguage),
/// YG2LocaleBridge переключает Unity Localization. Затем перезагружается текущая сцена,
/// чтобы все тексты перерисовались. Текущий язык подсвечивается (кнопка неактивна).
/// </summary>
public class LanguageSettingsPanel : MonoBehaviour
{
    [System.Serializable]
    public struct LanguageButton
    {
        [Tooltip("Код языка: ru, en или tr")]
        public string Code;
        public Button Button;
    }

    [SerializeField] private LanguageButton[] _buttons;

    private void Awake()
    {
        foreach (LanguageButton item in _buttons)
        {
            if (item.Button == null) continue;
            string code = item.Code;
            item.Button.onClick.AddListener(() => Choose(code));
        }
    }

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        foreach (LanguageButton item in _buttons)
        {
            if (item.Button != null) item.Button.interactable = item.Code != YG2.lang;
        }
    }

    public void Choose(string code)
    {
        if (string.IsNullOrEmpty(code) || code == YG2.lang) return;

        YG2.SwitchLanguage(code);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
