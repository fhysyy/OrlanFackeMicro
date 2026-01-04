using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.Runtime;
using System;

namespace FakeMicro.Tests.TestHelpers;

public abstract class GrainTestBase : IDisposable
{
    protected Mock<ILogger> LoggerMock { get; private set; }
    protected Mock<IGrainFactory> GrainFactoryMock { get; private set; }

    protected GrainTestBase()
    {
        SetupMocks();
    }

    private void SetupMocks()
    {
        LoggerMock = new Mock<ILogger>();
        GrainFactoryMock = new Mock<IGrainFactory>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LoggerMock?.Reset();
            GrainFactoryMock?.Reset();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
