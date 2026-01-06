using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;

namespace FakeMicro.Tests.IntegrationTests
{
    public class TestClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("UserStateStore");
            siloBuilder.AddMemoryGrainStorage("PubSubStore");

            siloBuilder.AddMemoryStreams("SMSProvider");
            siloBuilder.AddMemoryStreams("DefaultStream");
            siloBuilder.AddMemoryStreams("UserEventsStream");
            siloBuilder.AddMemoryStreams("MessageEventsStream");
            siloBuilder.AddMemoryStreams("AuthEventsStream");

            siloBuilder.UseInMemoryReminderService();

            // 注册服务依赖
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<FakeMicro.DatabaseAccess.IMessageRepository, FakeMicro.Tests.TestHelpers.MemoryMessageRepository>();
                services.AddSingleton<FakeMicro.Interfaces.Events.IEventPublisher, FakeMicro.Tests.TestHelpers.MemoryEventPublisher>();
            });

            siloBuilder.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "TestCluster";
                options.ServiceId = "TestService";
            });

            siloBuilder.Configure<SiloOptions>(options =>
            {
                options.SiloName = "TestSilo";
            });
        }

        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "TestCluster";
                options.ServiceId = "TestService";
            });

            clientBuilder.Configure<ClientMessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(30);
            });
        }
    }

    public class TestClusterFixture : IDisposable
    {
        public TestCluster Cluster { get; private set; }

        public TestClusterFixture()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestClusterConfigurator>();
            builder.AddClientBuilderConfigurator<TestClusterConfigurator>();
            builder.Options.InitialSilosCount = 1;

            Cluster = builder.Build();
            Cluster.Deploy();
        }

        public void Dispose()
        {
            Cluster?.StopAllSilosAsync().GetAwaiter().GetResult();
            Cluster?.Dispose();
        }
    }
}
