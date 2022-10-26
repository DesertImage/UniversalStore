using UnityEngine;

namespace UniStore
{
    public class UnifiedValidator : SimpleValidator
    {
        public UnifiedValidator(string url) : base(url)
        {
        }

        protected override string GetFinalReceipt(string receipt)
        {
            var unifiedReceipt = JsonUtility.FromJson<Receipt>(receipt);
            if (unifiedReceipt != null && !string.IsNullOrEmpty(unifiedReceipt.Payload))
            {
                return unifiedReceipt.Payload;
            }
            
            return base.GetFinalReceipt(receipt);
        }
    }
}