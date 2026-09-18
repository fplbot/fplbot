using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace FplBot.Tests.E2E;

public class LogCapture : ILogEventSink
{
    private readonly ConcurrentQueue<string> _messages = new();

    public void Emit(LogEvent logEvent) => _messages.Enqueue(logEvent.RenderMessage());

    public bool Contains(params string[] fragments) =>
        _messages.Any(m => fragments.All(f => m.Contains(f, StringComparison.OrdinalIgnoreCase)));

    public void Reset() => _messages.Clear();
}
