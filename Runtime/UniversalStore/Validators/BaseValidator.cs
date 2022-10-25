using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace UniStore
{
    public class BaseValidator : IValidator
    {
        private readonly string _url;

        public BaseValidator(string url)
        {
            _url = url;
        }

        public async Task Validate(string receipt, Action<bool> callback)
        {
            receipt = SetupReceipt(receipt);

            var form = new WWWForm();

            var webRequest = UnityWebRequest.Post
            (
                _url,
                SetupParams(form, receipt)
            );

            webRequest.downloadHandler = new DownloadHandlerBuffer();

            var process = webRequest.SendWebRequest();

            while (!process.isDone)
            {
                await Task.Yield();
            }

            var result = !webRequest.isHttpError && !webRequest.isNetworkError;
            if (result)
            {
#if DEBUG
                Debug.Log($"Response data:\n" + $"{webRequest.downloadHandler.text}");
#endif
                var response = JsonConvert.DeserializeObject<ValidationResponse>(webRequest.downloadHandler.text);

                result &= response?.Status == "0";
            }

            callback?.Invoke(result);
        }

        protected virtual string SetupReceipt(string receipt)
        {
            var unifiedReceipt = JsonUtility.FromJson<Receipt>(receipt);

            if (unifiedReceipt != null && !string.IsNullOrEmpty(unifiedReceipt.Payload))
            {
                return unifiedReceipt.Payload;
            }

            return receipt;
        }

        protected virtual WWWForm SetupParams(WWWForm form, string receipt)
        {
            var bundleId = Application.identifier;
            var userId = SystemInfo.deviceUniqueIdentifier;

            form.AddField("bundle_id", bundleId);
            form.AddField("user_id", userId);
            form.AddField("receipt", receipt);

            return form;
        }
    }
}