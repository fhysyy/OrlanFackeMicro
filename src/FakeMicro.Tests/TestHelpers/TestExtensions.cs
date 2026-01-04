using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Tests.TestHelpers;

public static class TestExtensions
{
    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<IGrainFactory> CreateGrainFactoryMock()
    {
        return new Mock<IGrainFactory>();
    }

    public static Mock<TGrainInterface> CreateGrainMock<TGrainInterface>()
        where TGrainInterface : class, IGrainWithStringKey
    {
        return new Mock<TGrainInterface>();
    }

    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string expectedMessage, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == logLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string expectedMessage)
    {
        loggerMock.VerifyLog(logLevel, expectedMessage, Times.Once());
    }

    public static async Task<T> ReturnsAsync<T>(this Task<T> task, T value)
    {
        await Task.CompletedTask;
        return value;
    }

    public static Mock<TGrainInterface> SetupGrainReference<TGrainInterface>(
        this Mock<IGrainFactory> grainFactoryMock,
        string grainKey,
        Mock<TGrainInterface> grainMock)
        where TGrainInterface : class, IGrainWithStringKey
    {
        grainFactoryMock
            .Setup(x => x.GetGrain<TGrainInterface>(grainKey, null))
            .Returns(grainMock.Object);
        
        return grainMock;
    }

    public static void VerifyGrainCall<TGrainInterface>(
        this Mock<IGrainFactory> grainFactoryMock,
        string grainKey,
        Times times)
        where TGrainInterface : class, IGrainWithStringKey
    {
        grainFactoryMock.Verify(
            x => x.GetGrain<TGrainInterface>(grainKey, null),
            times);
    }
}
