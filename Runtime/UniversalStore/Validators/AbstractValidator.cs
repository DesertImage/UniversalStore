using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace UniStore
{
    public abstract class AbstractValidator<TValidateResponse> : IValidator
    {
        private readonly string _url;

        public AbstractValidator(string url)
        {
            _url = url;
        }

        public async Task Validate(string receipt, Action<bool> callback)
        {
            receipt = GetFinalReceipt(receipt);

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

#if UNITY_2020_1_OR_NEWER
            var result = webRequest.result == UnityWebRequest.Result.Success;
#else
            var result = !webRequest.isHttpError && !webRequest.isNetworkError;
#endif
            if (result)
            {
#if DEBUG
                Debug.Log($"Response data:\n" + $"{webRequest.downloadHandler.text}");
#endif
                result = IsResponseValid
                (
                    JsonConvert.DeserializeObject<TValidateResponse>(webRequest.downloadHandler.text)
                );
            }

            callback?.Invoke(result);
        }

        protected abstract bool IsResponseValid(TValidateResponse response);

        protected virtual string GetFinalReceipt(string receipt) => receipt;

        protected virtual string GetBundleId() => Application.identifier;
        protected virtual string GetUserId() => SystemInfo.deviceUniqueIdentifier;

        protected virtual WWWForm SetupParams(WWWForm form, string receipt)
        {
            form.AddField("bundle_id", GetBundleId());
            form.AddField("user_id", GetUserId());
            form.AddField("receipt", receipt);

            return form;
        }
    }
}