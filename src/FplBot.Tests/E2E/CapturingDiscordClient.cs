using System.Collections.Concurrent;
using System.Net;
using Discord.Net.HttpClients;
using Discord.Net.HttpClients.Components;
using FplBot.Tests.E2E.Discord;

namespace FplBot.Tests.E2E;

public class CapturingDiscordClient(DiscordMessageCapture capture) : IDiscordClient
{
    private readonly ConcurrentDictionary<string, Exception> _failing = new();
    private IEnumerable<DiscordClient.Channel> _guildChannels = [];

    public void SetGuildChannels(params DiscordClient.Channel[] channels) => _guildChannels = channels;

    public void FailChannel(string channelId, int errorCode) =>
        _failing[channelId] = new DiscordApiException(
            errorCode == 10003 ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden, errorCode, $"stubbed {errorCode}");

    public void FailChannel(string channelId, HttpStatusCode status) =>
        _failing[channelId] = new DiscordApiException(status, null, $"stubbed {status}");

    public void FailChannel(string channelId, Exception exception) =>
        _failing[channelId] = exception;

    public void RecoverChannel(string channelId) => _failing.TryRemove(channelId, out _);

    public void Reset()
    {
        _failing.Clear();
        _guildChannels = [];
    }

    public Task ChannelMessagePost(string channelId, string text)
    {
        ThrowIfFailing(channelId);
        capture.Record(new DiscordCapturedMessage(channelId, text, null, null));
        return Task.CompletedTask;
    }

    public Task ChannelMessagePost(string channelId, ComponentRequest request)
    {
        ThrowIfFailing(channelId);
        var (title, description) = HeadingAndBody(request);
        capture.Record(new DiscordCapturedMessage(channelId, null, title, description));
        return Task.CompletedTask;
    }

    public Task InteractionFollowupPost(string interactionToken, ComponentRequest request)
    {
        var (title, description) = HeadingAndBody(request);
        capture.Record(new DiscordCapturedFollowup(interactionToken, title, description));
        return Task.CompletedTask;
    }

    private static (string? Heading, string? Body) HeadingAndBody(ComponentRequest request)
    {
        var texts = request.Components
            .OfType<Container>()
            .SelectMany(c => c.Components)
            .OfType<TextDisplay>()
            .Select(t => t.Content)
            .ToList();

        var heading = texts.ElementAtOrDefault(0)?.TrimStart('#').TrimStart();
        return (heading, string.Join("\n", texts.Skip(1)));
    }

    public Task ApplicationsCommandPost(string name, string description, string? guildId, params ApplicationCommandOptions[] options) =>
        Task.CompletedTask;

    public Task ApplicationsCommandForGuildDelete(string guildId, string commandId) => Task.CompletedTask;

    public Task<IEnumerable<DiscordClient.ApplicationsCommand>> ApplicationsCommandForGuildGet(string guildId) =>
        Task.FromResult(Enumerable.Empty<DiscordClient.ApplicationsCommand>());

    public Task<IEnumerable<DiscordClient.Channel>> GuildChannelsGet(string guildId) =>
        Task.FromResult(_guildChannels);

    public Task GuildLeave(string guildId)
    {
        ThrowIfFailing(guildId);
        capture.Record(new DiscordLeftGuild(guildId));
        return Task.CompletedTask;
    }

    private void ThrowIfFailing(string channelId)
    {
        if (_failing.TryGetValue(channelId, out var failure))
        {
            throw failure;
        }
    }
}
