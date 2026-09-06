using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess.Core;

namespace Chess.Presentation
{
    public class ShopPanel : MonoBehaviour
    {
        [Header("공통 UI")]
        public TextMeshProUGUI gemText;
        public TextMeshProUGUI goldText;
        public Button backgroundOverlay;
        public Button closeButton;

        [Header("탭")]
        public Button gemTabButton;
        public Button goldTabButton;
        public TextMeshProUGUI gemTabText;
        public TextMeshProUGUI goldTabText;
        public GameObject gemTabUnderline;
        public GameObject goldTabUnderline;
        public GameObject gemProducts;
        public GameObject goldProducts;

        [Header("젬 상품 버튼 (소 / 중 / 대 / 스타터)")]
        public Button buyButtonSmall;
        public Button buyButtonMedium;
        public Button buyButtonLarge;
        public Button buyButtonStarter;

        [Header("젬 상품 가격 텍스트 (스토어 연동 자동 갱신)")]
        public TextMeshProUGUI priceSmall;
        public TextMeshProUGUI priceMedium;
        public TextMeshProUGUI priceLarge;
        public TextMeshProUGUI priceStarter;

        [Header("골드 상품 버튼 (소 / 중 / 대 / 스타터)")]
        public Button buyGoldButtonSmall;
        public Button buyGoldButtonMedium;
        public Button buyGoldButtonLarge;
        public Button buyGoldButtonStarter;

        [Header("골드 상품 가격 텍스트 (스토어 연동 자동 갱신)")]
        public TextMeshProUGUI priceGoldSmall;
        public TextMeshProUGUI priceGoldMedium;
        public TextMeshProUGUI priceGoldLarge;
        public TextMeshProUGUI priceGoldStarter;

        [Header("로딩 오버레이")]
        public GameObject loadingOverlay;
        public TextMeshProUGUI loadingText;

        [Header("결과 팝업")]
        public GameObject resultPopup;
        public TextMeshProUGUI resultTitle;
        public TextMeshProUGUI resultDesc;
        public Button confirmButton;

        static readonly Color TAB_ACTIVE   = new Color(0.788f, 0.569f, 0.227f, 1f); // #C9913A
        static readonly Color TAB_INACTIVE = new Color(0.333f, 0.333f, 0.333f, 1f); // #555555

        // ─── 초기화 ────────────────────────────────────────────────

        void Start()
        {
            if (backgroundOverlay != null) backgroundOverlay.onClick.AddListener(Close);
            if (closeButton       != null) closeButton.onClick.AddListener(Close);

            if (gemTabButton  != null) gemTabButton.onClick.AddListener(()  => SwitchTab(true));
            if (goldTabButton != null) goldTabButton.onClick.AddListener(() => SwitchTab(false));

            if (buyButtonSmall   != null) buyButtonSmall.onClick.AddListener(()   => Buy(IAPManager.GEM_SMALL));
            if (buyButtonMedium  != null) buyButtonMedium.onClick.AddListener(()  => Buy(IAPManager.GEM_MEDIUM));
            if (buyButtonLarge   != null) buyButtonLarge.onClick.AddListener(()   => Buy(IAPManager.GEM_LARGE));
            if (buyButtonStarter != null) buyButtonStarter.onClick.AddListener(() => Buy(IAPManager.STARTER_PACK));

            if (buyGoldButtonSmall   != null) buyGoldButtonSmall.onClick.AddListener(()   => Buy(IAPManager.CURRENCY_SMALL));
            if (buyGoldButtonMedium  != null) buyGoldButtonMedium.onClick.AddListener(()  => Buy(IAPManager.CURRENCY_MEDIUM));
            if (buyGoldButtonLarge   != null) buyGoldButtonLarge.onClick.AddListener(()   => Buy(IAPManager.CURRENCY_LARGE));
            if (buyGoldButtonStarter != null) buyGoldButtonStarter.onClick.AddListener(() => Buy(IAPManager.CURRENCY_STARTER));

            if (confirmButton != null) confirmButton.onClick.AddListener(CloseResultPopup);

            var iap = IAPManager.Instance;
            if (iap != null)
            {
                iap.OnPurchaseSuccess += OnSuccess;
                iap.OnPurchaseError  += OnFailed;
            }

            // ── 초기 상태 설정 ──────────────────────────────────────
            // loadingOverlay, resultPopup은 Inspector에서 비활성(SetActive false)으로 설정할 것
            // gemProducts 활성, goldProducts 비활성으로 설정할 것
            // GemTab/Underline 활성, GoldTab/Underline 비활성으로 설정할 것
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
            SwitchTab(true); // GemTab 기본
            RefreshGemText();
            RefreshGoldText();
            RefreshPrices();
            CloseResultPopup();
        }

        void SwitchTab(bool isGemTab)
        {
            if (gemProducts  != null) gemProducts.SetActive(isGemTab);
            if (goldProducts != null) goldProducts.SetActive(!isGemTab);

            if (gemTabText  != null) gemTabText.color  = isGemTab ? TAB_ACTIVE : TAB_INACTIVE;
            if (goldTabText != null) goldTabText.color  = isGemTab ? TAB_INACTIVE : TAB_ACTIVE;

            if (gemTabUnderline  != null) gemTabUnderline.SetActive(isGemTab);
            if (goldTabUnderline != null) goldTabUnderline.SetActive(!isGemTab);
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
            IAPManager.Instance?.BuyProduct(productId);
        }

        void OnSuccess(string productId)
        {
            SetLoading(false);
            SetButtons(true);
            RefreshGemText();
            RefreshGoldText();

            int gems     = IAPManager.GetGemAmount(productId);
            int currency = IAPManager.GetCurrencyAmount(productId);
            string desc  = gems > 0
                ? $"{gems:N0} 로열 젬이 지급되었습니다"
                : $"{currency:N0} 로얄이 지급되었습니다";

            ShowResultPopup("구매 완료", desc);
        }

        void OnFailed(string message)
        {
            SetLoading(false);
            SetButtons(true);
            ShowResultPopup("구매 실패", "다시 시도해주세요");
        }

        // ─── 유틸 ──────────────────────────────────────────────────

        void RefreshGemText()
        {
            if (gemText != null && PlayerInventory.Instance != null)
                gemText.text = $"{PlayerInventory.Instance.gems:N0} 젬";
        }

        void RefreshGoldText()
        {
            if (goldText != null && PlayerInventory.Instance != null)
                goldText.text = $"{PlayerInventory.Instance.currency:N0} 로얄";
        }

        void RefreshPrices()
        {
            var iap = IAPManager.Instance;
            if (iap == null || !iap.IsInitialized) return;
            if (priceSmall   != null) priceSmall.text   = iap.GetLocalizedPrice(IAPManager.GEM_SMALL);
            if (priceMedium  != null) priceMedium.text  = iap.GetLocalizedPrice(IAPManager.GEM_MEDIUM);
            if (priceLarge   != null) priceLarge.text   = iap.GetLocalizedPrice(IAPManager.GEM_LARGE);
            if (priceStarter != null) priceStarter.text = iap.GetLocalizedPrice(IAPManager.STARTER_PACK);

            if (priceGoldSmall   != null) priceGoldSmall.text   = iap.GetLocalizedPrice(IAPManager.CURRENCY_SMALL);
            if (priceGoldMedium  != null) priceGoldMedium.text  = iap.GetLocalizedPrice(IAPManager.CURRENCY_MEDIUM);
            if (priceGoldLarge   != null) priceGoldLarge.text   = iap.GetLocalizedPrice(IAPManager.CURRENCY_LARGE);
            if (priceGoldStarter != null) priceGoldStarter.text = iap.GetLocalizedPrice(IAPManager.CURRENCY_STARTER);
        }

        void SetLoading(bool show)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(show);
            if (loadingText    != null) loadingText.text = "구매 처리 중...";
        }

        void SetButtons(bool interactable)
        {
            if (buyButtonSmall      != null) buyButtonSmall.interactable      = interactable;
            if (buyButtonMedium     != null) buyButtonMedium.interactable     = interactable;
            if (buyButtonLarge      != null) buyButtonLarge.interactable      = interactable;
            if (buyButtonStarter    != null) buyButtonStarter.interactable    = interactable;
            if (buyGoldButtonSmall   != null) buyGoldButtonSmall.interactable   = interactable;
            if (buyGoldButtonMedium  != null) buyGoldButtonMedium.interactable  = interactable;
            if (buyGoldButtonLarge   != null) buyGoldButtonLarge.interactable   = interactable;
            if (buyGoldButtonStarter != null) buyGoldButtonStarter.interactable = interactable;
        }

        void ShowResultPopup(string title, string desc)
        {
            if (resultTitle != null) resultTitle.text = title;
            if (resultDesc  != null) resultDesc.text  = desc;
            if (resultPopup != null) resultPopup.SetActive(true);
        }

        void CloseResultPopup()
        {
            if (resultPopup != null) resultPopup.SetActive(false);
        }
    }
}
