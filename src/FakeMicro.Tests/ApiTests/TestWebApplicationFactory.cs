using FakeMicro.Api;
using FakeMicro.Tests.IntegrationTests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.TestingHost;
using System;
using System.Linq;

namespace FakeMicro.Tests.ApiTests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly TestCluster _testCluster;

        public TestWebApplicationFactory()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<TestClusterConfigurator>();
            builder.AddClientBuilderConfigurator<TestClusterConfigurator>();
            builder.Options.InitialSilosCount = 1;

            _testCluster = builder.Build();
            _testCluster.Deploy();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IClusterClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IClusterClient>(_testCluster.Client);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _testCluster?.StopAllSilosAsync().GetAwaiter().GetResult();
                _testCluster?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
