#if HUAWEI
using System;
using System.Collections.Generic;
using System.Linq;
using HmsPlugin;
using HuaweiMobileServices.IAP;
using HuaweiMobileServices.Utils;

namespace UniStore
{
    public class HuaweiStore : BaseStore, IInitable
    {
        // Please insert your products via custom editor. You can find it in Huawei > Kit Settings > IAP tab.
        public event Action<bool> OnInitialized;
        public bool IsInitialized { get; private set; }

        private List<InAppPurchaseData> _purchasedProducts;

        private Dictionary<string, ProductInfo> _productInfos;

        private string _purchaseInProcess;

        public HuaweiStore(IEnumerable<IAPProduct> products, IValidator validator = null) : base(products, validator)
        {
            _productInfos = new Dictionary<string, ProductInfo>();
        }

        public void Initialize()
        {
            HMSIAPManager.Instance.OnBuyProductSuccess += OnBuyProductSuccess;
            HMSIAPManager.Instance.OnBuyProductFailure += OnBuyProductFailure;

            HMSIAPManager.Instance.OnCheckIapAvailabilitySuccess += OnCheckIapAvailabilitySuccess;
            HMSIAPManager.Instance.OnCheckIapAvailabilityFailure += OnCheckIapAvailabilityFailure;

            // Uncomment below if InitializeOnStart is not enabled in Huawei > Kit Settings > IAP tab.
            //HMSIAPManager.Instance.CheckIapAvailability();
        }

        public override bool IsPurchased(string id)
        {
            return _purchasedProducts?.FirstOrDefault(x => x.ProductId == id) != null;
        }

        public override string GetPrice(string id)
        {
            return !_productInfos.TryGetValue(id, out var info) ? "fake_price" : info.Price;
        }

        protected override void BuyProcess(string id)
        {
            _purchaseInProcess = id;

            HMSIAPManager.Instance.BuyProduct(id);
        }

        #region RESTORE

        public override void RestorePurchases()
        {
            HMSIAPManager.Instance.RestorePurchases
            (
                restoredProducts =>
                {
                    _purchasedProducts = new List<InAppPurchaseData>(restoredProducts.InAppPurchaseDataList);
                    Restored(string.IsNullOrEmpty(restoredProducts.ErrMsg));
                }
            );
        }

        public override void TryRestorePurchases(Action<bool> callback)
        {
            HMSIAPManager.Instance.RestorePurchases
            (
                restoredProducts =>
                {
                    _purchasedProducts = new List<InAppPurchaseData>(restoredProducts.InAppPurchaseDataList);

                    callback?.Invoke(string.IsNullOrEmpty(restoredProducts.ErrMsg));
                }
            );
        }

        #endregion

        public override IStore CreateNewInstance()
        {
            return new HuaweiStore(Products?.Values, Validator);
        }

        #region CALLBACKS

        private void OnBuyProductSuccess(PurchaseResultInfo arg)
        {
            _purchaseInProcess = default;

            ValidationProcess
            (
                arg.InAppPurchaseDataRawJSON,
                arg.InAppPurchaseData.ProductId,
                result =>
                {
                    PurchaseSuccess
                    (
                        new PurchaseInfo
                        {
                            ProductId = arg.InAppPurchaseData.ProductId,
                            Price = arg.InAppPurchaseData.Price.ToString(),
                            Currency = arg.InAppPurchaseData.Currency,
                        },
                        arg.InAppPurchaseDataRawJSON
                    );
                }
            );
        }

        private void OnBuyProductFailure(int returnCode)
        {
#if DEBUG
            UnityEngine.Debug.LogError($"<b>[HuaweiStore]</b> purchase failed. Return code: {returnCode.ToString()}");
#endif
            var productInfo = HMSIAPManager.Instance.GetProductInfo(_purchaseInProcess);

            PurchaseFailed
            (
                new PurchaseInfo
                {
                    ProductId = _purchaseInProcess,
                    Price = productInfo.Price,
                    Currency = productInfo.Currency
                },
                returnCode.ToString()
            );

            _purchaseInProcess = default;
        }

        private void OnCheckIapAvailabilityFailure(HMSException obj)
        {
#if DEBUG
            UnityEngine.Debug.LogError("<b>[HuaweiStore]</b> IAP is not ready");
#endif
        }

        private void OnCheckIapAvailabilitySuccess()
        {
#if DEBUG
            UnityEngine.Debug.LogError("<b>[HuaweiStore]</b> IAP is ready");
#endif
            OnInitialized?.Invoke(true);
        }

        #endregion
    }
}

#endif