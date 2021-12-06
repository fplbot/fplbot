using Microsoft.Extensions.Logging;

namespace FplBot.Tests.Helpers;

public class XUnitTestOutputLogger<T> : ILogger<T>, IDisposable
{
    private readonly ITestOutputHelper _helper;

    public XUnitTestOutputLogger(ITestOutputHelper helper = null)
    {
        _helper = helper;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _helper?.WriteLine(state.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }

    public void Dispose()
    {
            
    }
}
