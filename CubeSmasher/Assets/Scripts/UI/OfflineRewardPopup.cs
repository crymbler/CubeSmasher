using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Infrastructure;
using YG;

public class OfflineRewardPopup : MonoBehaviour
{
    private const string AdId = "offline_reward";

    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Button _claimButton;
    [SerializeField] private Button _doubleButton;

    private IWallet _wallet;
    private IWallet WalletRef => _wallet ??= ServiceLocator.Instance.Get<IWallet>();

    private double _amount;
    private bool _awaitingAd;

    private void Awake()
    {
        _panel.SetActive(false);
        _claimButton.onClick.AddListener(OnClaimClicked);
        _doubleButton.onClick.AddListener(OnDoubleClicked);
    }

    private void OnEnable()
    {
        YG2.onRewardAdv += OnRewardSuccess;
        YG2.onCloseRewardedAdv += OnAdClosed;
        YG2.onErrorRewardedAdv += OnAdError;
    }

    private void OnDisable()
    {
        YG2.onRewardAdv -= OnRewardSuccess;
        YG2.onCloseRewardedAdv -= OnAdClosed;
        YG2.onErrorRewardedAdv -= OnAdError;
    }

    public void Show(double amount)
    {
        if (amount <= 0) return;

        _amount = amount;
        _amountText.text = NumberFormat.Short(amount);
        _claimButton.interactable = true;
        _doubleButton.interactable = true;
        _panel.SetActive(true);
    }

    private void OnClaimClicked()
    {
        Grant(_amount);
        Close();
    }

    private void OnDoubleClicked()
    {
        _awaitingAd = true;
        _claimButton.interactable = false;
        _doubleButton.interactable = false;
        YG2.RewardedAdvShow(AdId);
    }

    private void OnRewardSuccess(string id)
    {
        if (id != AdId) return;

        _awaitingAd = false;
        Grant(_amount * 2);
        Close();
    }

    private void OnAdClosed()
    {
        if (!_awaitingAd) return;
        _awaitingAd = false;
        Grant(_amount);
        Close();
    }

    private void OnAdError()
    {
        if (!_awaitingAd) return;
        _awaitingAd = false;
        Grant(_amount);
        Close();
    }

    private void Grant(double amount)
    {
        WalletRef.Add(amount);
        DevTrace.Money("offline", amount);
    }

    private void Close()
    {
        _panel.SetActive(false);
        _amount = 0;
        YG2.saves.pendingOfflineReward = 0;
        if (ServiceLocator.Instance.TryGet(out ISaveService saveService))
        {
            saveService.Save();
        }
    }
}