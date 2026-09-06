using System.Threading.Channels;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Tests.E2E;

public class SlackMessageCapture
{
    private Channel<ChatPostMessageRequest> _channel = System.Threading.Channels.Channel.CreateUnbounded<ChatPostMessageRequest>();

    public void Record(ChatPostMessageRequest req) => _channel.Writer.TryWrite(req);

    public async Task<ChatPostMessageRequest> WaitForMessageAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        return await _channel.Reader.ReadAsync(cts.Token);
    }

    public void Reset()
    {
        _channel = System.Threading.Channels.Channel.CreateUnbounded<ChatPostMessageRequest>();
    }
}
