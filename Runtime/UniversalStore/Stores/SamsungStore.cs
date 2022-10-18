#if SAMSUNG
using System;
using System.Collections.Generic;
using System.Linq;
using Samsung;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniStore
{
    public class SamsungStore : BaseStore, IInitable
    {
        private const string SetOperationModeMethod = "setOperationMode";
        private const string GetProductDetailsMethod = "getProductDetails";
        private const string StartPaymentMethod = "startPayment";
        private const string ConsumePurchasedItemsMethod = "consumePurchasedItems";
        
        public Action<ProductInfoList> OnGetProductsDetailsListener;
        public Action<PurchasedInfo> OnStartPaymentListener;
        public Action<ConsumedList> OnConsumePurchasedItemListener;
        public Action<OwnedProductList> OnGetOwnedListListener;

        public event Action<bool> OnInitialized;

        public bool IsInitialized => true;

        private readonly HashSet<string> _purchased;

        private readonly Dictionary<string, ProductVo> _productInfos;

        private AndroidJavaObject _iapInstance;

        private string _savedPassthroughParam = "";

        public SamsungStore(IEnumerable<IAPProduct> products, IValidator validator = null) :
            base(products, validator)
        {
            _purchased = new HashSet<string>();
            _productInfos = new Dictionary<string, ProductVo>();
        }

        public void Initialize()
        {
            using (var cls = new AndroidJavaClass("com.samsung.android.sdk.iap.lib.activity.SamsungIAPFragment"))
            {
                cls.CallStatic("init", ToString());

                _iapInstance = cls.CallStatic<AndroidJavaObject>("getInstance");

                SetOperationMode
                (
#if DEBUG
                    OperationMode.OPERATION_MODE_TEST
#else
                    OperationMode.OPERATION_MODE_PRODUCTION
#endif
                );

                OnInitialized?.Invoke(_iapInstance != null);

                var ids = Products.Aggregate(string.Empty, (current, pair) => current + pair.Key + ", ");
                GetProductsDetails
                (
                    ids,
                    productInfoList =>
                    {
                        foreach (var result in productInfoList.results)
                        {
                            _productInfos.Add(result.mItemId, result);
                        }
                    }
                );
            }
        }

        public override bool IsPurchased(string id) => _purchased.Contains(id);

        public override string GetPrice(string id)
        {
            return !_productInfos.TryGetValue(id, out var info) ? "$0.01 (fake)" : info.mItemPrice;
        }

        protected override void BuyProcess(string id) => StartPayment(id, "OnPayment", OnPayment);

        #region RESTORE

        public override void RestorePurchases() => TryRestorePurchases(Restored);
        public override void TryRestorePurchases(Action<bool> callback) => callback?.Invoke(true);

        #endregion

        public override IStore CreateNewInstance() => new SamsungStore(Products?.Values, Validator);

        private void CallNativeMethod(string name, params string[] arguments)
        {
            if (_iapInstance != null)
            {
                _iapInstance.Call(name, arguments);
            }
            else
            {
#if DEBUG
                Debug.LogError("<b>[SamsungStore]</b> Android Context not initialized correctly.");
#endif
            }
        }

        #region IAP Functions

        private static PurchaseInfo ConvertToProduct(PurchaseVo purchaseVo)
        {
            return new PurchaseInfo
            {
                ProductId = purchaseVo.mItemId,
                Price = purchaseVo.mItemPriceString,
                Currency = purchaseVo.mCurrencyCode,
                Type = purchaseVo.mConsumableYN == "Y" ? ProductType.Consumable : ProductType.NonConsumable
            };
        }

        private static PurchaseInfo ConvertToProduct(OwnedProductVo productVo)
        {
            return new PurchaseInfo
            {
                ProductId = productVo.mItemId,
                Price = productVo.mItemPriceString,
                Currency = productVo.mCurrencyCode,
                Type = productVo.mConsumableYN == "Y" ? ProductType.Consumable : ProductType.NonConsumable
            };
        }

        public void SetOperationMode(OperationMode mode)
        {
            CallNativeMethod(SetOperationModeMethod, mode.ToString());
        }

        public void GetProductsDetails(string itemIDs, Action<ProductInfoList> listener)
        {
            OnGetProductsDetailsListener = listener;

            CallNativeMethod(GetProductDetailsMethod, itemIDs);
        }

//         public void GetOwnedList(ItemType itemType, Action<OwnedProductList> listener)
//         {
//             OnGetOwnedListListener = listener;
//
//             if (_iapInstance != null)
//             {
//                 _iapInstance.Call("getOwnedList", itemType.ToString());
//             }
//             else
//             {
// #if DEBUG
//                 Debug.LogError("<b>[SamsungStore]</b> Android Context not initialized correctly.");
// #endif
//             }
//         }


        private void StartPayment(string itemID, string passThroughParam, Action<PurchasedInfo> listener)
        {
            _savedPassthroughParam = passThroughParam;
            OnStartPaymentListener = listener;

            CallNativeMethod(StartPaymentMethod, itemID, passThroughParam);
        }

        private void ConsumePurchasedItems(string purchaseIDs, Action<ConsumedList> listener)
        {
            OnConsumePurchasedItemListener = listener;

            CallNativeMethod(ConsumePurchasedItemsMethod, purchaseIDs);
        }

        #endregion

        #region CALLBACKS

        public void OnGetProductsDetails(string resultJSON)
        {
            var productList = JsonUtility.FromJson<ProductInfoList>(resultJSON);
#if DEBUG
            Debug.Log($"<b>[SamsungStore]</b> OnGetProductsDetails : {resultJSON}");
            Debug.Log($"<b>[SamsungStore]</b> OnGetProductsDetails cnt: {productList.results.Count}");

            for (var i = 0; i < productList.results.Count; ++i)
            {
                Debug.Log(
                    $"<b>[SamsungStore]</b> onGetProductsDetails: {productList.results[i].mItemName}");
            }
#endif
            OnGetProductsDetailsListener?.Invoke(productList);
        }

        private void OnGetOwnedProducts(string resultJSON)
        {
            var ownedList = JsonUtility.FromJson<OwnedProductList>(resultJSON);

#if DEBUG
            Debug.Log($"<b>[SamsungStore]</b> onGetOwnedProducts cnt: {ownedList.results.Count}");

            foreach (var productVo in ownedList.results)
            {
                Debug.Log("onGetOwnedProducts: " + productVo.mItemName);
            }
#endif
            OnGetOwnedListListener?.Invoke(ownedList);

            foreach (var productVo in ownedList.results)
            {
                PurchaseSuccess(ConvertToProduct(productVo), string.Empty);
            }
        }

        private void OnConsumePurchasedItems(string resultJSON)
        {
            var consumedList = JsonUtility.FromJson<ConsumedList>(resultJSON);

#if DEBUG
            Debug.Log($"<b>[SamsungStore]</b> OnConsumePurchasedItems: {resultJSON}");
            Debug.Log($"<b>[SamsungStore]</b> OnConsumePurchasedItems cnt: {consumedList.results.Count}");

            foreach (var consumeResult in consumedList.results)
            {
                Debug.Log("<b>[SamsungStore]</b> OnConsumePurchasedItems: " + consumeResult.mPurchaseId);
            }
#endif
            OnConsumePurchasedItemListener?.Invoke(consumedList);
        }

        private void OnPayment(PurchasedInfo purchasedInfo)
        {
            if ((purchasedInfo.errorInfo?.errorCode ?? 1) != 0) return;
            if (purchasedInfo.results == null) return;
            
            _purchased.Add(purchasedInfo.results.mItemId);
#if DEBUG
            if (purchasedInfo.results.mPassThroughParam != _savedPassthroughParam)
            {
                Debug.Log("<b>[SamsungStore]</b> PassThroughParam is different!!!");
            }
#endif
            OnStartPaymentListener?.Invoke(purchasedInfo);
            
            PurchaseSuccess(ConvertToProduct(purchasedInfo.results), string.Empty);

            if (purchasedInfo.results.mConsumableYN == "Y")
            {
                ConsumePurchasedItems(purchasedInfo.results.mItemId, OnConsumePurchasedItemListener);
            }
        }

        #endregion
    }
}
#endif