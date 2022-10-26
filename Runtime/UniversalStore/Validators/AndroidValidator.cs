using System;
using System.Text;

namespace UniStore
{
    public class AndroidValidator : SimpleValidator
    {
        public AndroidValidator(string url) : base(url)
        {
        }

        protected override string GetFinalReceipt(string receipt)
        {
            var bytesToEncode = Encoding.UTF8.GetBytes(receipt);

            return Convert.ToBase64String(bytesToEncode);
        }
    }
}