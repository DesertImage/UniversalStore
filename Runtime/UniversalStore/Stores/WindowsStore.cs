#if !HUAWEI && !SAMSUNG
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace UniStore
{
    public class WindowsStore : UnityPurchasingStore
    {
        public WindowsStore(IEnumerable<IAPProduct> products, IValidator validator = null) : base(products, validator)
        {
        }

        protected override void SetupBuilder(ConfigurationBuilder builder)
        {
            base.SetupBuilder(builder);
#if DEBUG
            builder.Configure<IMicrosoftConfiguration>().useMockBillingSystem = true;
#else
            builder.Configure<IMicrosoftConfiguration>().useMockBillingSystem = false    ;
#endif
        }
    }
}
#endif