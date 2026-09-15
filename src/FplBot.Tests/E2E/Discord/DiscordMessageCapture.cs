using System.Threading.Channels;

namespace FplBot.Tests.E2E.Discord;

public record DiscordCapturedMessage(string ChannelId, string? Text, string? Title, string? Description);

public class DiscordMessageCapture
{
    private Channel<DiscordCapturedMessage> _channel = System.Threading.Channels.Channel.CreateUnbounded<DiscordCapturedMessage>();

    public void Record(DiscordCapturedMessage message) => _channel.Writer.TryWrite(message);

    public async Task<DiscordCapturedMessage> WaitForMessageAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        return await _channel.Reader.ReadAsync(cts.Token);
    }

    /// <summary>
    /// Like <see cref="WaitForMessageAsync(TimeSpan?)"/>, but discards messages posted to other
    /// Discord channels instead of returning them — use this when a test's own channel could
    /// otherwise see a stray message left in flight by another test sharing this capture.
    /// </summary>
    public async Task<DiscordCapturedMessage> WaitForMessageAsync(string channelId, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var msg = await _channel.Reader.ReadAsync(cts.Token);
            if (msg.ChannelId == channelId)
                return msg;
        }
    }

    public void Reset()
    {
        _channel = System.Threading.Channels.Channel.CreateUnbounded<DiscordCapturedMessage>();
    }
}
