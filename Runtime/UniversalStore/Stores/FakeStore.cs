using System;
using System.Collections.Generic;

namespace UniStore
{
    public class FakeStore : BaseStore
    {
        private readonly HashSet<string> _purchased;

        public FakeStore(IEnumerable<IAPProduct> products, IValidator validator = null) : base(products, validator)
        {
            _purchased = new HashSet<string>();
        }

        public override bool IsPurchased(string id) => _purchased.Contains(id);
        public override string GetPrice(string id) => "$0.01 (fake)";

        protected override void BuyProcess(string id)
        {
            _purchased.Add(id);

            PurchaseSuccess
            (
                new PurchaseInfo
                {
                    ProductId = id,
                    Price = GetPrice(id)
                },
                string.Empty
            );
        }

        #region RESTORE

        public override void RestorePurchases() => TryRestorePurchases(Restored);

        public override void TryRestorePurchases(Action<bool> callback) => callback?.Invoke(true);

        #endregion

        public override IStore CreateNewInstance() => new FakeStore(Products?.Values, Validator);
    }
}