using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniStore
{
    public abstract class BaseStore : IStore
    {
        public event Action<PurchaseInfo> OnPurchaseStarted;

        public event Action<PurchaseInfo, string> OnPurchaseSuccess;
        public event Action<PurchaseInfo, string> OnPurchaseFailed;

        public event Action<bool> OnRestore;

        public IDictionary<string, IAPProduct> Products { get; }

        protected readonly IValidator Validator;

        protected BaseStore(IEnumerable<IAPProduct> products, IValidator validator = null)
        {
            Products = new Dictionary<string, IAPProduct>();
            foreach (var product in products)
            {
                Products.Add(product.Id, product);
            }

            Validator = validator;
        }

        public abstract bool IsPurchased(string id);

        public abstract string GetPrice(string id);

        public void Buy(string id)
        {
            OnPurchaseStarted?.Invoke
            (
                new PurchaseInfo
                {
                    ProductId = id,
                    Price = GetPrice(id)
                }
            );

            BuyProcess(id);
        }

        protected abstract void BuyProcess(string id);

        #region RESTORE

        public abstract void RestorePurchases();

        public abstract void TryRestorePurchases(Action<bool> callback);

        #endregion

        public abstract IStore CreateNewInstance();

        #region VALIDATION

        protected async Task ValidationProcess(string receipt, string productId, Action<bool> callback)
        {
            if (Validator == null)
            {
                callback?.Invoke(true);
                return;
            }

            await Validator.Validate(receipt, callback);
        }

        #endregion

        #region EVENTS

        // protected void Initialized(bool result) => OnInitialized?.Invoke(result);

        protected void PurchaseStarted(PurchaseInfo purchaseInfo)
        {
            OnPurchaseStarted?.Invoke(purchaseInfo);
        }

        protected void PurchaseSuccess(PurchaseInfo purchaseInfo, string receipt)
        {
            OnPurchaseSuccess?.Invoke(purchaseInfo, receipt);
        }

        protected void PurchaseFailed(PurchaseInfo purchaseInfo, string failureReason)
        {
            OnPurchaseFailed?.Invoke(purchaseInfo, failureReason);
        }

        protected void Restored(bool result)
        {
            OnRestore?.Invoke(result);
        }

        #endregion
    }
}