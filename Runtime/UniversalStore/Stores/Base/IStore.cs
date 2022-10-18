using System;
using System.Collections.Generic;

namespace UniStore
{
    public interface IStore
    {
        event Action<PurchaseInfo> OnPurchaseStarted;
        event Action<PurchaseInfo, string> OnPurchaseSuccess;
        event Action<PurchaseInfo, string> OnPurchaseFailed;

        event Action<bool> OnRestore;

        IDictionary<string, IAPProduct> Products { get; }

        bool IsPurchased(string id);

        string GetPrice(string id);

        void Buy(string id);

        void RestorePurchases();
        void TryRestorePurchases(Action<bool> callback);

        IStore CreateNewInstance();
    }
}