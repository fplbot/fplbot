using System.Collections;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Helpers;

public class TestPublishEndpoint : IPublishEndpoint
{
    public record PublishedMessage(object Message);

    private readonly List<PublishedMessage> _messages = new();
    public PublishedMessageCollection PublishedMessages => new(_messages);

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish<T>(object message, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish<T>(object message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish<T>(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        _messages.Add(new PublishedMessage(message));
        return Task.CompletedTask;
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotImplementedException();
}

public class TestScopeFactory(TestPublishEndpoint publisher) : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new TestScope(publisher);

    private class TestScope(TestPublishEndpoint publisher) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(publisher);
        public void Dispose() { }
    }

    private class TestServiceProvider(TestPublishEndpoint publisher) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IPublishEndpoint) ? publisher : null;
    }
}

public class PublishedMessageCollection(List<TestPublishEndpoint.PublishedMessage> messages)
    : IEnumerable<TestPublishEndpoint.PublishedMessage>
{
    public TestPublishEndpoint.PublishedMessage this[int index] => messages[index];
    public int Length => messages.Count;
    public int Count => messages.Count;

    public IEnumerable<TestPublishEndpoint.PublishedMessage> Containing<T>() =>
        messages.Where(m => m.Message is T);

    public IEnumerator<TestPublishEndpoint.PublishedMessage> GetEnumerator() => messages.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => messages.GetEnumerator();
}
