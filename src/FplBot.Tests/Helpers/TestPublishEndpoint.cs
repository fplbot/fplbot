#nullable enable
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

public class TestScopeFactory : IServiceScopeFactory
{
    private readonly TestPublishEndpoint _publisher;
    public TestScopeFactory(TestPublishEndpoint publisher) => _publisher = publisher;

    public IServiceScope CreateScope() => new TestScope(_publisher);

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

public class PublishedMessageCollection : IEnumerable<TestPublishEndpoint.PublishedMessage>
{
    private readonly List<TestPublishEndpoint.PublishedMessage> _messages;

    public PublishedMessageCollection(List<TestPublishEndpoint.PublishedMessage> messages) => _messages = messages;

    public TestPublishEndpoint.PublishedMessage this[int index] => _messages[index];
    public int Length => _messages.Count;
    public int Count => _messages.Count;

    public IEnumerable<TestPublishEndpoint.PublishedMessage> Containing<T>() =>
        _messages.Where(m => m.Message is T);

    public IEnumerator<TestPublishEndpoint.PublishedMessage> GetEnumerator() => _messages.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _messages.GetEnumerator();
}
