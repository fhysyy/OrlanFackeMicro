using Orleans;

namespace FakeMicro.Interfaces
{
    public interface IHelloGrain : IGrainWithStringKey
    {
        Task<string> SayHelloAsync(string greeting);
    }
}