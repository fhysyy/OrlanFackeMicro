using Orleans;
using Orleans.Concurrency;

namespace FakeMicro.Interfaces
{
    public interface ICounterGrain : IGrainWithStringKey
    {
        Task<int> IncrementAsync();
        Task<int> DecrementAsync();
        [ReadOnly]
        [AlwaysInterleave]
        Task<int> GetCountAsync();
        Task ResetAsync();
    }
}