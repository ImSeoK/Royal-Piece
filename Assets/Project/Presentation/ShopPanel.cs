using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Chess.Core;

namespace Chess.Presentation
{
    public class ShopPanel : MonoBehaviour
    {
        [Header("공통 UI")]
        public TextMeshProUGUI goldText;
        public Button closeButton;
        public Button backgroundOverlay;
        public GameObject loadingOverlay;

        [Header("상품 버튼 (순서: 소량 / 중량 / 대량 / 스타터)")]
        public Button buyButtonSmall;
        public Button buyButtonMedium;
        public Button buyButtonLarge;
        public Button buyButtonStarter;

        [Header("상품 가격 텍스트 (스토어 연동 자동 갱신)")]
        public TextMeshProUGUI priceSmall;
        public TextMeshProUGUI priceMedium;
        public TextMeshProUGUI priceLarge;
        public TextMeshProUGUI priceStarter;

        [Header("결과 메시지")]
        public TextMeshProUGUI resultText;

        // ─── 초기화 ────────────────────────────────────────────────

        void Start()
        {
            if (closeButton      != null) closeButton.onClick.AddListener(Close);
            if (backgroundOverlay != null) backgroundOverlay.onClick.AddListener(Close);

            if (buyButtonSmall   != null) buyButtonSmall.onClick.AddListener(()  => Buy(IAPManager.GOLD_SMALL));
            if (buyButtonMedium  != null) buyButtonMedium.onClick.AddListener(() => Buy(IAPManager.GOLD_MEDIUM));
            if (buyButtonLarge   != null) buyButtonLarge.onClick.AddListener(()  => Buy(IAPManager.GOLD_LARGE));
            if (buyButtonStarter != null) buyButtonStarter.onClick.AddListener(() => Buy(IAPManager.STARTER_PACK));

            var iap = IAPManager.Instance;
            if (iap != null)
            {
                iap.OnPurchaseSuccess += OnSuccess;
                iap.OnPurchaseError  += OnFailed;
            }
        }

        void OnDestroy()
        {
            var iap = IAPManager.Instance;
            if (iap == null) return;
            iap.OnPurchaseSuccess -= OnSuccess;
            iap.OnPurchaseError  -= OnFailed;
        }

        // ─── 열기 / 닫기 ───────────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshGoldText();
            RefreshPrices();
            SetResult("");
        }

        void Close()
        {
            gameObject.SetActive(false);
        }

        // ─── 구매 ──────────────────────────────────────────────────

        void Buy(string productId)
        {
            SetButtons(false);
            SetLoading(true);
            SetResult("");
            IAPManager.Instance?.BuyProduct(productId);
        }

        void OnSuccess(string productId)
        {
            SetLoading(false);
            SetButtons(true);
            RefreshGoldText();
            int gold = IAPManager.GetGoldAmount(productId);
            SetResult($"+{gold:N0} 골드 획득!");
            StartCoroutine(ClearResultAfter(3f));
        }

        void OnFailed(string message)
        {
            SetLoading(false);
            SetButtons(true);
            SetResult(message);
            StartCoroutine(ClearResultAfter(3f));
        }

        // ─── 유틸 ──────────────────────────────────────────────────

        void RefreshGoldText()
        {
            if (goldText != null && PlayerInventory.Instance != null)
                goldText.text = $"{PlayerInventory.Instance.currency:N0} G";
        }

        void RefreshPrices()
        {
            var iap = IAPManager.Instance;
            if (iap == null || !iap.IsInitialized) return;
            if (priceSmall   != null) priceSmall.text   = iap.GetLocalizedPrice(IAPManager.GOLD_SMALL);
            if (priceMedium  != null) priceMedium.text  = iap.GetLocalizedPrice(IAPManager.GOLD_MEDIUM);
            if (priceLarge   != null) priceLarge.text   = iap.GetLocalizedPrice(IAPManager.GOLD_LARGE);
            if (priceStarter != null) priceStarter.text = iap.GetLocalizedPrice(IAPManager.STARTER_PACK);
        }

        void SetLoading(bool show)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(show);
        }

        void SetButtons(bool interactable)
        {
            if (buyButtonSmall   != null) buyButtonSmall.interactable   = interactable;
            if (buyButtonMedium  != null) buyButtonMedium.interactable  = interactable;
            if (buyButtonLarge   != null) buyButtonLarge.interactable   = interactable;
            if (buyButtonStarter != null) buyButtonStarter.interactable = interactable;
        }

        void SetResult(string msg)
        {
            if (resultText != null) resultText.text = msg;
        }

        IEnumerator ClearResultAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SetResult("");
        }
    }
}
