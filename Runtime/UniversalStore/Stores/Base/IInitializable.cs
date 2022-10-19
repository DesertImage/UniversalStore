using System;

namespace UniStore
{
    public interface IInitializable
    {
        event Action<bool> OnInitialized;

        bool IsInitialized { get; }
        

        void Initialize();
    }
}