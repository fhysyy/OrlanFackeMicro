using Orleans;
using Orleans.Concurrency;

namespace FakeMicro.Interfaces
{
    public interface IHelloGrain : IGrainWithStringKey
    {
        [ReadOnly]
        [AlwaysInterleave]
        Task<string> SayHelloAsync(string greeting);
    }
}