using Orleans;
using Orleans.Streams;

namespace FakeMicro.Interfaces.Eventing
{
    public interface IStreamProviderGrain : IGrainWithStringKey
    {
        Task<IStreamProvider> GetStreamProviderAsync(string providerName = "DefaultStream");
    }
}
