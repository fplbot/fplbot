using System.Collections.Concurrent;
using System.Net;
using Discord.Net.HttpClients;
using FplBot.Tests.E2E.Discord;

namespace FplBot.Tests.E2E;

public class CapturingDiscordClient(DiscordMessageCapture capture) : IDiscordClient
{
    private readonly ConcurrentDictionary<string, DiscordApiException> _failing = new();

    public void FailChannel(string channelId, int errorCode) =>
        _failing[channelId] = new DiscordApiException(
            errorCode == 10003 ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden, errorCode, $"stubbed {errorCode}");

    public void FailChannel(string channelId, HttpStatusCode status) =>
        _failing[channelId] = new DiscordApiException(status, null, $"stubbed {status}");

    public void RecoverChannel(string channelId) => _failing.TryRemove(channelId, out _);

    public void Reset() => _failing.Clear();

    public Task ChannelMessagePost(string channelId, string text)
    {
        ThrowIfFailing(channelId);
        capture.Record(new DiscordCapturedMessage(channelId, text, null, null));
        return Task.CompletedTask;
    }

    public Task ChannelMessagePost(string channelId, DiscordClient.RichEmbed embed)
    {
        ThrowIfFailing(channelId);
        capture.Record(new DiscordCapturedMessage(channelId, null, embed.Title, embed.Description));
        return Task.CompletedTask;
    }

    public Task ApplicationsCommandPost(string name, string description, string? guildId, params ApplicationCommandOptions[] options) =>
        Task.CompletedTask;

    public Task ApplicationsCommandForGuildDelete(string guildId, string commandId) => Task.CompletedTask;

    public Task<IEnumerable<DiscordClient.ApplicationsCommand>> ApplicationsCommandForGuildGet(string guildId) =>
        Task.FromResult(Enumerable.Empty<DiscordClient.ApplicationsCommand>());

    public Task<IEnumerable<DiscordClient.Channel>> GuildChannelsGet(string guildId) =>
        Task.FromResult(Enumerable.Empty<DiscordClient.Channel>());

    private void ThrowIfFailing(string channelId)
    {
        if (_failing.TryGetValue(channelId, out var failure))
        {
            throw failure;
        }
    }
}
