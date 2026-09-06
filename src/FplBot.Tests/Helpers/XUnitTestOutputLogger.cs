using Microsoft.Extensions.Logging;

namespace FplBot.Tests.Helpers;

public class XUnitTestOutputLogger<T>(ITestOutputHelper? helper = null) : ILogger<T>, IDisposable
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        helper?.WriteLine(state?.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public void Dispose()
    {
    }
}
