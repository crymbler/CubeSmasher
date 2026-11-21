using UnityEngine;
using UnityEngine.UI;
using YG;

namespace CubeSmasher.UI
{
    [RequireComponent(typeof(Button))]
    public class RemoveAdsButton : MonoBehaviour
    {
        private const string ProductId = "RemoveAd";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Buy);
        }

        private void OnEnable()
        {
            YG2.onPurchaseSuccess += OnPurchaseSuccess;
            YG2.onGetSDKData += Refresh;
            YG2.onGetPayments += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            YG2.onPurchaseSuccess -= OnPurchaseSuccess;
            YG2.onGetSDKData -= Refresh;
            YG2.onGetPayments -= Refresh;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(Buy);
        }

        private void Refresh()
        {
            if (!YG2.saves.isAdsRemoved && IsOwned()) GrantAdsRemoval();
            if (YG2.saves.isAdsRemoved) gameObject.SetActive(false);
        }

        private static bool IsOwned()
        {
            var purchase = YG2.PurchaseByID(ProductId);
            return purchase != null && !purchase.consumed;
        }

        private static void GrantAdsRemoval()
        {
            YG2.saves.isAdsRemoved = true;
            YG2.SaveProgress();
        }

        private void Buy()
        {
            if (YG2.saves.isAdsRemoved) return;
            YG2.BuyPayments(ProductId);
        }

        private void OnPurchaseSuccess(string purchasedId)
        {
            if (purchasedId != ProductId) return;

            GrantAdsRemoval();
            Refresh();
        }
    }
}
