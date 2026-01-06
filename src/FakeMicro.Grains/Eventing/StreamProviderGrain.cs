using FakeMicro.Interfaces.Eventing;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace FakeMicro.Grains.Eventing
{
    public class StreamProviderGrain : OrleansGrainBase, IStreamProviderGrain
    {
        public StreamProviderGrain(ILogger<StreamProviderGrain> logger) : base(logger)
        {
        }

        public Task<IStreamProvider> GetStreamProviderAsync(string providerName = "DefaultStream")
        {
            try
            {
                var provider = this.GetStreamProvider(providerName);
                _logger.LogInformation($"成功获取流提供程序: {providerName}");
                return Task.FromResult(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取流提供程序失败: {providerName}");
                throw;
            }
        }
    }
}
