using System.Threading.Channels;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

public class SlackMessageCapture
{
    private Channel<ChatPostMessageRequest> _channel = System.Threading.Channels.Channel.CreateUnbounded<ChatPostMessageRequest>();

    public void Record(ChatPostMessageRequest req) => _channel.Writer.TryWrite(req);

    public async Task<ChatPostMessageRequest> WaitForMessageAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        return await _channel.Reader.ReadAsync(cts.Token);
    }

    /// <summary>
    /// Like <see cref="WaitForMessageAsync(TimeSpan?)"/>, but discards messages posted to other
    /// Slack channels instead of returning them — use this when a test's own channel could
    /// otherwise see a stray message left in flight by another test sharing this capture.
    /// </summary>
    public async Task<ChatPostMessageRequest> WaitForMessageAsync(string channel, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var msg = await _channel.Reader.ReadAsync(cts.Token);
            if (msg.Channel == channel)
                return msg;
        }
    }

    public void Reset()
    {
        _channel = System.Threading.Channels.Channel.CreateUnbounded<ChatPostMessageRequest>();
    }
}
