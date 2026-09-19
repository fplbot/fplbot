using System.Threading.Channels;

namespace FplBot.Tests.E2E.Discord;

public record DiscordCapturedMessage(string ChannelId, string? Text, string? Title, string? Description);

public record DiscordCapturedFollowup(string InteractionToken, string? Title, string? Description);

public class DiscordMessageCapture
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private Channel<DiscordCapturedMessage> _channel = System.Threading.Channels.Channel.CreateUnbounded<DiscordCapturedMessage>();
    private Channel<DiscordCapturedFollowup> _followups = System.Threading.Channels.Channel.CreateUnbounded<DiscordCapturedFollowup>();

    public void Record(DiscordCapturedMessage message) => _channel.Writer.TryWrite(message);

    public async Task<DiscordCapturedMessage> WaitForMessageAsync(TimeSpan? timeout = null)
    {
        var waitFor = timeout ?? DefaultTimeout;
        using var cts = new CancellationTokenSource(waitFor);
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"No Discord message arrived within {waitFor}.");
        }
    }

    /// <summary>
    /// Like <see cref="WaitForMessageAsync(TimeSpan?)"/>, but discards messages posted to other
    /// Discord channels instead of returning them — use this when a test's own channel could
    /// otherwise see a stray message left in flight by another test sharing this capture.
    /// </summary>
    public async Task<DiscordCapturedMessage> WaitForMessageAsync(string channelId, TimeSpan? timeout = null)
    {
        var waitFor = timeout ?? DefaultTimeout;
        using var cts = new CancellationTokenSource(waitFor);
        var otherChannels = 0;
        try
        {
            while (true)
            {
                var msg = await _channel.Reader.ReadAsync(cts.Token);
                if (msg.ChannelId == channelId)
                    return msg;
                otherChannels++;
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"No Discord message for channel {channelId} within {waitFor} ({otherChannels} message(s) arrived for other channels).");
        }
    }

    public void Record(DiscordCapturedFollowup followup) => _followups.Writer.TryWrite(followup);

    /// <summary>
    /// Waits for the interaction followup posted against <paramref name="interactionToken"/>,
    /// discarding followups belonging to other interactions. A followup carries no channel, so
    /// the token is the only thing that ties it back to the test that triggered it.
    /// </summary>
    public async Task<DiscordCapturedFollowup> WaitForFollowupAsync(string interactionToken, TimeSpan? timeout = null)
    {
        var waitFor = timeout ?? DefaultTimeout;
        using var cts = new CancellationTokenSource(waitFor);
        try
        {
            while (true)
            {
                var followup = await _followups.Reader.ReadAsync(cts.Token);
                if (followup.InteractionToken == interactionToken)
                    return followup;
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"No Discord followup for interaction {interactionToken} within {waitFor}.");
        }
    }

    // Non-blocking counterpart to WaitForMessageAsync, for asserting that nothing was posted
    // once the bus is idle. Drains what is queued so a stray message from another test sharing
    // this capture doesn't answer for this one.
    public bool AnyMessage(string? channelId = null)
    {
        var any = false;
        while (_channel.Reader.TryRead(out var msg))
            any |= channelId is null || msg.ChannelId == channelId;

        return any;
    }

    public void Reset()
    {
        _channel = System.Threading.Channels.Channel.CreateUnbounded<DiscordCapturedMessage>();
        _followups = System.Threading.Channels.Channel.CreateUnbounded<DiscordCapturedFollowup>();
    }
}
