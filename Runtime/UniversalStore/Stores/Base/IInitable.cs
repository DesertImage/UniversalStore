using System;

namespace UniStore
{
    public interface IInitable
    {
        event Action<bool> OnInitialized;

        bool IsInitialized { get; }
        

        void Initialize();
    }
}