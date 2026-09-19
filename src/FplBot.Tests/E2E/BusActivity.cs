using MassTransit;

namespace FplBot.Tests.E2E;

// Tracks what the in-memory bus is doing, so a test asserting that nothing was posted can wait
// for the handlers to actually finish instead of sleeping for an arbitrary grace period.
public class BusActivity : IConsumeObserver
{
    private int _inFlight;
    private long _consumed;

    public (int InFlight, long Consumed) Snapshot() => (Volatile.Read(ref _inFlight), Interlocked.Read(ref _consumed));

    public Task PreConsume<T>(ConsumeContext<T> context) where T : class
    {
        Interlocked.Increment(ref _inFlight);
        return Task.CompletedTask;
    }

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class => Done();

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class => Done();

    private Task Done()
    {
        Interlocked.Increment(ref _consumed);
        Interlocked.Decrement(ref _inFlight);
        return Task.CompletedTask;
    }
}
