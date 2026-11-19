using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class AdCountdownPanel : MonoBehaviour
{
    public static AdCountdownPanel Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _label;

    private CanvasGroup _group;
    private Coroutine _routine;

    public bool IsPlaying => _routine != null;

    private void Awake()
    {
        Instance = this;
        _group = GetComponent<CanvasGroup>();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(Action onComplete)
    {
        if (IsPlaying) return;
        _routine = StartCoroutine(Run(onComplete));
    }

    private IEnumerator Run(Action onComplete)
    {
        _group.alpha = 1f;
        _group.blocksRaycasts = true;

        for (int n = 2; n >= 1; n--)
        {
            _label.text = n.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        Hide();
        _routine = null;
        onComplete?.Invoke();
    }

    private void Hide()
    {
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }
}
