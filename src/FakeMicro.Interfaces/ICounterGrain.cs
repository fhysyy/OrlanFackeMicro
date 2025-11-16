using Orleans;

namespace FakeMicro.Interfaces
{
    public interface ICounterGrain : IGrainWithStringKey
    {
        Task<int> IncrementAsync();
        Task<int> DecrementAsync();
        Task<int> GetCountAsync();
        Task ResetAsync();
    }
}