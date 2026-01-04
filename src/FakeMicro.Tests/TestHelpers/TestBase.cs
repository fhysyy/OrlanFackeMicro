using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FakeMicro.Tests.TestHelpers;

public abstract class TestBase : IDisposable
{
    protected Mock<ILogger> LoggerMock { get; private set; }

    protected TestBase()
    {
        LoggerMock = new Mock<ILogger>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LoggerMock?.Reset();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void AssertLog(LogLevel expectedLevel, string expectedMessage, Times? times = null)
    {
        var callTimes = times ?? Times.Once();
        
        LoggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == expectedLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            callTimes);
    }
}
