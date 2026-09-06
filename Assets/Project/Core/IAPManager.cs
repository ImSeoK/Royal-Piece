using UnityEngine;
using UnityEngine.Purchasing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chess.Core
{
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        // ─── 상품 ID (App Store Connect / Google Play Console과 정확히 일치해야 함) ───
        public const string GEM_SMALL      = "gem_small";
        public const string GEM_MEDIUM     = "gem_medium";
        public const string GEM_LARGE      = "gem_large";
        public const string STARTER_PACK   = "starter_pack";

        public const string CURRENCY_SMALL   = "currency_small";
        public const string CURRENCY_MEDIUM  = "currency_medium";
        public const string CURRENCY_LARGE   = "currency_large";
        public const string CURRENCY_STARTER = "currency_starter";

        public static int GetGemAmount(string productId) => productId switch
        {
            GEM_SMALL    => 1000,
            GEM_MEDIUM   => 6000,
            GEM_LARGE    => 15000,
            STARTER_PACK => 3000,
            _            => 0,
        };

        public static int GetCurrencyAmount(string productId) => productId switch
        {
            CURRENCY_SMALL   => 5000,
            CURRENCY_MEDIUM  => 30000,
            CURRENCY_LARGE   => 80000,
            CURRENCY_STARTER => 15000,
            _                => 0,
        };

        public bool IsInitialized { get; private set; }

        public event Action<string> OnPurchaseSuccess;
        public event Action<string> OnPurchaseError;

        IProductService  _productService;
        IPurchaseService _purchaseService;

        // ─── 초기화 ────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await UnityIAPServices.DefaultStore().Connect();

            _productService = UnityIAPServices.DefaultProduct();
            _productService.OnProductsFetched     += OnProductsFetched;
            _productService.OnProductsFetchFailed += OnProductsFetchFailed;

            _purchaseService = UnityIAPServices.DefaultPurchase();
            _purchaseService.OnPurchasePending   += OnPurchasePending;
            _purchaseService.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _purchaseService.OnPurchaseFailed    += OnPurchaseFailed;

            _productService.FetchProducts(new List<ProductDefinition>
            {
                new ProductDefinition(GEM_SMALL,       ProductType.Consumable),
                new ProductDefinition(GEM_MEDIUM,      ProductType.Consumable),
                new ProductDefinition(GEM_LARGE,       ProductType.Consumable),
                new ProductDefinition(STARTER_PACK,    ProductType.Consumable),
                new ProductDefinition(CURRENCY_SMALL,   ProductType.Consumable),
                new ProductDefinition(CURRENCY_MEDIUM,  ProductType.Consumable),
                new ProductDefinition(CURRENCY_LARGE,   ProductType.Consumable),
                new ProductDefinition(CURRENCY_STARTER, ProductType.Consumable),
            });
        }

        void OnDestroy()
        {
            if (_productService != null)
            {
                _productService.OnProductsFetched     -= OnProductsFetched;
                _productService.OnProductsFetchFailed -= OnProductsFetchFailed;
            }
            if (_purchaseService != null)
            {
                _purchaseService.OnPurchasePending   -= OnPurchasePending;
                _purchaseService.OnPurchaseConfirmed -= OnPurchaseConfirmed;
                _purchaseService.OnPurchaseFailed    -= OnPurchaseFailed;
            }
        }

        // ─── 상품 로드 콜백 ────────────────────────────────────────

        void OnProductsFetched(List<Product> products)
        {
            IsInitialized = true;
            Debug.Log($"[IAP] 초기화 완료: {products.Count}개 상품");
        }

        void OnProductsFetchFailed(ProductFetchFailed failed)
        {
            Debug.LogWarning($"[IAP] 상품 로드 실패: {failed.FailureReason}");
        }

        // ─── 구매 요청 ─────────────────────────────────────────────

        public void BuyProduct(string productId)
        {
            if (!IsInitialized)
            {
                OnPurchaseError?.Invoke("스토어 연결 중입니다. 잠시 후 다시 시도해주세요.");
                return;
            }
            _purchaseService.PurchaseProduct(productId);
        }

        // ─── 구매 콜백 ─────────────────────────────────────────────

        void OnPurchasePending(PendingOrder pendingOrder)
        {
            string productId = pendingOrder.CartOrdered.Items()
                ?.FirstOrDefault()?.Product?.definition?.id ?? "";

            Debug.Log($"[IAP] 구매 대기: {productId}");

            // TODO: 출시 전 반드시 Cloud Function 영수증 검증으로 교체
            int gems = GetGemAmount(productId);
            if (gems > 0 && PlayerInventory.Instance != null)
                PlayerInventory.Instance.AddGems(gems);

            int currency = GetCurrencyAmount(productId);
            if (currency > 0 && PlayerInventory.Instance != null)
                PlayerInventory.Instance.AddCurrency(currency);

            _purchaseService.ConfirmPurchase(pendingOrder);
        }

        void OnPurchaseConfirmed(Order confirmedOrder)
        {
            string productId = confirmedOrder.CartOrdered.Items()
                ?.FirstOrDefault()?.Product?.definition?.id ?? "";

            Debug.Log($"[IAP] 구매 확인 완료: {productId}");
            OnPurchaseSuccess?.Invoke(productId);
        }

        void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.LogWarning($"[IAP] 구매 실패: {failedOrder.FailureReason}");
            OnPurchaseError?.Invoke(GetFailureMessage(failedOrder.FailureReason.ToString()));
        }

        // ─── 유틸 ──────────────────────────────────────────────────

        public string GetLocalizedPrice(string productId)
        {
            if (!IsInitialized) return "";
            return _productService?.GetProductById(productId)?.metadata?.localizedPriceString ?? "";
        }

        static string GetFailureMessage(string reason) => reason switch
        {
            "UserCancelled"        => "구매가 취소되었습니다.",
            "PaymentDeclined"      => "결제가 거절되었습니다.",
            "DuplicateTransaction" => "중복 결제입니다.",
            _                      => "구매에 실패했습니다. 다시 시도해주세요.",
        };
    }
}
