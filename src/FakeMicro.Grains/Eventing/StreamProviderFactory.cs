using FakeMicro.Interfaces.Eventing;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using System;

namespace FakeMicro.Grains.Eventing
{
    public interface IStreamProviderFactory
    {
        Task<IStreamProvider> GetStreamProviderAsync(string providerName = "DefaultStream");
    }

    public class StreamProviderFactory : IStreamProviderFactory
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ILoggerService _logger;

        public StreamProviderFactory(
            IGrainFactory grainFactory,
            ILoggerService logger)
        {
            _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IStreamProvider> GetStreamProviderAsync(string providerName = "DefaultStream")
        {
            try
            {
                var streamProviderGrain = _grainFactory.GetGrain<IStreamProviderGrain>(providerName);
                var provider = await streamProviderGrain.GetStreamProviderAsync(providerName);
                _logger.LogInformation($"成功获取流提供程序: {providerName}");
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取流提供程序失败: {providerName}");
                throw;
            }
        }
    }
}
