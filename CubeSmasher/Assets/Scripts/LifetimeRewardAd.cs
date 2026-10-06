using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Infrastructure;
using YG;

public class LifetimeRewardAd : MonoBehaviour
{
    // Wallet — обычный C# класс, Unity его не сериализует,
    // поэтому берём сервис из контейнера при первом обращении
    private IWallet _wallet;
    private IWallet WalletRef => _wallet ??= ServiceLocator.Instance.Get<IWallet>();

    [Header("Ссылки на компоненты")]
    [SerializeField] private GameObject _rewardPanel; // Корневой объект подарка (LifetimeReward)
    [SerializeField] private Button _rewardButton;
    [SerializeField] private Slider _timerSlider;
    [SerializeField] private TextMeshProUGUI _rewardText;

    [Header("Настройки")]
    [SerializeField] private string _rewardId = "lifetime_reward"; // ID рекламы
    [SerializeField] private float _cooldownNormal = 180f; // КД после получения или игнорирования (3 минуты)
    [SerializeField] private float _cooldownPenalty = 60f; // КД при пропуске или ошибке рекламы (1 минута)
    [SerializeField] private float _activeDuration = 15f; // Сколько секунд подарок висит на экране

    private Coroutine _stateCoroutine;
    private double _currentRewardAmount;
    private bool _isRewarded;
    private bool _isAdShowing; // Защита: вызвал ли рекламу именно этот скрипт?

    private void OnEnable()
    {
        // Подписываемся на события PluginYG
        YG2.onRewardAdv += OnRewardSuccess;
        YG2.onCloseRewardedAdv += OnRewardClosed;
        YG2.onErrorRewardedAdv += OnRewardError;
    }

    private void OnDisable()
    {
        // Обязательно отписываемся, чтобы избежать утечек памяти
        YG2.onRewardAdv -= OnRewardSuccess;
        YG2.onCloseRewardedAdv -= OnRewardClosed;
        YG2.onErrorRewardedAdv -= OnRewardError;
    }

    private void Start()
    {
        // Назначаем метод на кнопку через код
        _rewardButton.onClick.AddListener(OnRewardButtonClicked);
        
        // Сразу запускаем первые 3 минуты
        StartCooldown(_cooldownNormal);
    }

    // Универсальный метод для запуска таймера ожидания
    private void StartCooldown(float waitTime)
    {
        if (_stateCoroutine != null) 
            StopCoroutine(_stateCoroutine);
            
        _rewardPanel.SetActive(false);
        _stateCoroutine = StartCoroutine(CooldownRoutine(waitTime));
    }

    // Корутина ожидания (3 минуты или 60 сек)
    private IEnumerator CooldownRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ShowGift();
    }

    // Показ подарка
    private void ShowGift()
    {
        // Берем ровно 10% от текущего баланса
        _currentRewardAmount = WalletRef.Balance * 0.1;
        
        // Обновляем текст (F0 округлит число для красивого отображения в UI, без десятых долей)
        _rewardText.text = $"+ {_currentRewardAmount:F0}";

        _rewardButton.interactable = true;
        _rewardPanel.SetActive(true);

        // Запускаем 15-секундный таймер
        if (_stateCoroutine != null) 
            StopCoroutine(_stateCoroutine);
            
        _stateCoroutine = StartCoroutine(ActiveGiftRoutine());
    }

    // Корутина активности подарка (15 секунд ползунка)
    private IEnumerator ActiveGiftRoutine()
    {
        float elapsed = 0f;
        _timerSlider.maxValue = _activeDuration;
        _timerSlider.value = _activeDuration;

        while (elapsed < _activeDuration)
        {
            elapsed += Time.deltaTime;
            _timerSlider.value = _activeDuration - elapsed;
            yield return null;
        }

        // Если цикл завершился, а игрок не нажал — прячем подарок и ждем снова 3 минуты
        StartCooldown(_cooldownNormal);
    }

    // Клик по подарку
    private void OnRewardButtonClicked()
    {
        // 1. Отключаем кнопку (защита от двойного клика)
        _rewardButton.interactable = false;
        
        // 2. Останавливаем 15-секундный таймер, чтобы подарок не пропал во время показа
        if (_stateCoroutine != null) 
            StopCoroutine(_stateCoroutine);
            
        // 3. Подготавливаем флаги и вызываем рекламу
        _isRewarded = false;
        _isAdShowing = true;
        YG2.RewardedAdvShow(_rewardId);
    }

    // Ивент: Реклама успешно просмотрена до конца
    private void OnRewardSuccess(string id)
    {
        if (id == _rewardId)
        {
            _isRewarded = true;
            WalletRef.Add(_currentRewardAmount);
        }
    }

    // Ивент: Реклама закрыта (неважно, досмотрел или нет)
    private void OnRewardClosed()
    {
        if (!_isAdShowing) return; // Игнорируем, если закрылась реклама из другого места игры
        _isAdShowing = false;

        // Если награда получена — КД 3 минуты, если скипнул — КД 60 секунд
        if (_isRewarded)
        {
            StartCooldown(_cooldownNormal);
        }
        else
        {
            StartCooldown(_cooldownPenalty);
        }
    }

    // Ивент: Ошибка загрузки/показа рекламы (например, нет интернета)
    private void OnRewardError()
    {
        if (!_isAdShowing) return;
        _isAdShowing = false;

        // При ошибке также даем штрафной кулдаун 60 секунд
        StartCooldown(_cooldownPenalty);
    }
}