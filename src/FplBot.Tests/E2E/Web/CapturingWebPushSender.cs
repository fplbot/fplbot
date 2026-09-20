using System.Collections.Concurrent;
using System.Threading.Channels;
using FplBot.Domain;
using FplBot.Integrations.WebPush;

namespace FplBot.Tests.E2E.Web;

public record CapturedWebPush(string Endpoint, string Title, string Body, string? Link);

public class CapturingWebPushSender : IWebPushSender
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<string, WebPushOutcome> _outcomes = new();
    private readonly List<CapturedWebPush> _unclaimed = [];
    private Channel<CapturedWebPush> _channel = System.Threading.Channels.Channel.CreateUnbounded<CapturedWebPush>();

    public void FailEndpointAsGone(string endpoint) => _outcomes[endpoint] = WebPushOutcome.Gone;

    public void FailEndpoint(string endpoint) => _outcomes[endpoint] = WebPushOutcome.Failed;

    public void Reset()
    {
        _outcomes.Clear();
        lock (_unclaimed)
        {
            _unclaimed.Clear();
        }

        _channel = System.Threading.Channels.Channel.CreateUnbounded<CapturedWebPush>();
    }

    public Task<WebPushOutcome> Send(PushKeys keys, WebPushPayload payload)
    {
        var outcome = _outcomes.GetValueOrDefault(keys.Endpoint, WebPushOutcome.Delivered);
        if (outcome == WebPushOutcome.Delivered)
        {
            _channel.Writer.TryWrite(new CapturedWebPush(keys.Endpoint, payload.Title, payload.Body, payload.Link));
        }

        return Task.FromResult(outcome);
    }

    public async Task<CapturedWebPush> WaitForAsync(string endpoint, TimeSpan? timeout = null)
    {
        var waitFor = timeout ?? DefaultTimeout;
        using var cts = new CancellationTokenSource(waitFor);
        try
        {
            if (TryClaim(endpoint) is { } claimed)
            {
                return claimed;
            }

            while (true)
            {
                var push = await _channel.Reader.ReadAsync(cts.Token);
                if (push.Endpoint == endpoint)
                {
                    return push;
                }

                lock (_unclaimed)
                {
                    _unclaimed.Add(push);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"No web push for {endpoint} arrived within {waitFor}.");
        }
    }

    private CapturedWebPush? TryClaim(string endpoint)
    {
        lock (_unclaimed)
        {
            if (_unclaimed.FirstOrDefault(p => p.Endpoint == endpoint) is { } push)
            {
                _unclaimed.Remove(push);
                return push;
            }
        }

        return null;
    }

    public bool Any()
    {
        lock (_unclaimed)
        {
            return _unclaimed.Count > 0 || _channel.Reader.Count > 0;
        }
    }
}
