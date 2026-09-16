using Discord.Net.HttpClients;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Discord;
using FplBot.Domain;
using FplBot.EventHandlers.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record GuildWithSubsDto(string GuildId, string GuildName, IEnumerable<ChannelSubscriptionDto> Subscriptions);

public record DiscordBroadcastRequest(string Message, ChannelFilter Filter);

public record UpdateGuildChannelSubscriptionsRequest(IEnumerable<EventSubscription> Subscriptions);

public record MoveGuildChannelRequest(string NewChannelId);

public static class AdminDiscordEndpoints
{
    // Slash commands are only ever managed for this one hardcoded test guild today —
    // carried over unchanged from Pages/Admin/Discord/Slashcommands.cshtml.cs.
    private const string TestGuildId = "893932860162064414";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/discord/slashcommands", GetSlashCommands);
        group.MapGet("/discord/slashcommands/definitions", GetSlashCommandDefinitions);
        group.MapPost("/discord/slashcommands/install", InstallSlashCommands);
        group.MapPost("/discord/slashcommands/install-global", InstallGlobalSlashCommands);
        group.MapPost("/discord/slashcommands/uninstall", UninstallSlashCommands);

        group.MapGet("/discord/servers", GetSubscriptions);
        group.MapDelete("/discord/servers/{guildId}/{channelId}", DeleteSubscription);
        group.MapDelete("/discord/guilds/{guildId}/subscriptions", DeleteAllSubscriptionsForGuild);
        group.MapDelete("/discord/guilds/{guildId}", DeleteGuild);

        group.MapGet("/discord/guilds/{guildId}", GetGuild);
        group.MapPost("/discord/guilds/{guildId}/channels/{channelId}/publish-standings", PublishStandings);
        group.MapPut("/discord/guilds/{guildId}/channels/{channelId}/subscriptions", UpdateChannelSubscriptions);
        group.MapPut("/discord/guilds/{guildId}/channels/{channelId}/channel", MoveChannel);

        group.MapPost("/discord/broadcast", BroadcastToDiscord);
    }

    private static async Task<IResult> BroadcastToDiscord(DiscordBroadcastRequest request, ISendEndpointProvider sendEndpointProvider, ILogger<Program> logger)
    {
        logger.LogInformation("ENQUEUEING BROADCAST TO DISCORD");
        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(BroadcastHandler)}"));
            await endpoint.Send(new BroadcastToDiscord(request.Message, request.Filter));
            return TypedResults.Ok(new { message = $"Discord Broadcast enqueued using {request.Filter}!" });
        }
        catch (Exception e)
        {
            return TypedResults.Ok(new { message = $"Broadcast to Discord failed '{e}'" });
        }
    }

    private static Task<IResult> GetSlashCommands(DiscordSlashCommandsEnsurer ensurer, ILogger<Program> logger) =>
        CallDiscord(
            async () => TypedResults.Ok(await ensurer.GetAllForGuild(TestGuildId)),
            "fetch slash commands from Discord", logger);

    private static IResult GetSlashCommandDefinitions()
    {
        return TypedResults.Ok(DiscordSlashCommandsEnsurer.GetDefinedCommandSummaries());
    }

    private static Task<IResult> InstallSlashCommands(DiscordSlashCommandsEnsurer ensurer, ILogger<Program> logger) =>
        CallDiscord(async () =>
        {
            await ensurer.InstallGuildSlashCommandsInGuild(TestGuildId);
            return TypedResults.Ok(new { message = "Install queued!" });
        }, "install slash commands to the test guild", logger);

    private static Task<IResult> InstallGlobalSlashCommands(DiscordSlashCommandsEnsurer ensurer, ILogger<Program> logger) =>
        CallDiscord(async () =>
        {
            await ensurer.InstallGuildSlashCommandsInGuild();
            return TypedResults.Ok(new { message = "Global install queued!" });
        }, "install slash commands globally", logger);

    private static Task<IResult> UninstallSlashCommands(DiscordSlashCommandsEnsurer ensurer, ILogger<Program> logger) =>
        CallDiscord(async () =>
        {
            await ensurer.DeleteGuildSlashCommands(TestGuildId);
            return TypedResults.Ok(new { message = "Uninstall queued!" });
        }, "uninstall slash commands from the test guild", logger);

    // Discord's HTTP client throws HttpRequestException on any non-2xx response (e.g. 401
    // from an invalid/expired bot token) — surfaced here as 502 Bad Gateway, distinct from
    // our own 500s: this means *we* are fine, an upstream dependency (Discord) rejected us.
    // The detail is safe to show — it's Discord's status text, not our internals.
    private static async Task<IResult> CallDiscord(Func<Task<IResult>> action, string actionDescription, ILogger logger)
    {
        try
        {
            return await action();
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Failed to {Action}", actionDescription);
            return TypedResults.Problem(
                title: $"Failed to {actionDescription}",
                detail: $"Discord API request failed: {e.Message}",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    internal static async Task<IResult> GetSubscriptions(string? query, int page, int pageSize, IGuildRepository repo)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

        var installations = (await repo.GetAllInstallations()).ToList();

        var guildsWithSubs = installations
            .Select(i => new GuildWithSubsDto(i.Id, i.Name, i.ChannelSubscriptions.Select(c => ToDto(i.Id, c))))
            .ToList();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? guildsWithSubs
            : guildsWithSubs.Where(g =>
                g.GuildName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                g.GuildId.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return TypedResults.Ok(new PagedResult<GuildWithSubsDto>(items, page, pageSize, filtered.Count));
    }

    private static ChannelSubscriptionDto ToDto(string guildId, ChannelSubscription channel) =>
        new(guildId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null,
            ToEventSubscriptions(channel), channel.FailureCount, channel.FailingSince, channel.LastFailureReason);

    private static IEnumerable<EventSubscription> ToEventSubscriptions(ChannelSubscription? channel) =>
        channel?.Events.Current.Select(e => Enum.Parse<EventSubscription>(e.ToString())) ?? [];

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());

    internal static async Task<IResult> GetGuild(
        string guildId,
        IGuildRepository repo,
        ILeagueClient leagueClient,
        IDiscordClient discordClient,
        ILogger<Program> logger)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        IEnumerable<long>? guildChannelIds = null;
        try
        {
            var guildChannels = await discordClient.GuildChannelsGet(guildId);
            guildChannelIds = guildChannels.Select(c => c.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

        var channels = new List<object>();
        foreach (var channel in installation.ChannelSubscriptions)
        {
            var leagueId = channel.FollowedLeagueId?.Value;

            string? leagueName = null;
            if (leagueId.HasValue)
            {
                var league = await leagueClient.GetClassicLeague((int)leagueId.Value, tolerate404: true);
                leagueName = league?.Properties?.Name;
            }

            var channelStatus = guildChannelIds?.Any(id => id.ToString() == channel.ChannelId);

            channels.Add(new
            {
                channel = channel.ChannelId,
                leagueId,
                leagueName,
                subscriptions = ToEventSubscriptions(channel),
                channelStatus,
                failureCount = channel.FailureCount,
                failingSince = channel.FailingSince,
                lastFailureReason = channel.LastFailureReason,
                purgeEligibleAt = channel.FailingSince is { } since ? since + ChannelSubscription.MaxFailureAge : (DateTimeOffset?)null,
                failuresUntilPurge = Math.Max(0, ChannelSubscription.MaxFailures - channel.FailureCount)
            });
        }

        return TypedResults.Ok(new
        {
            guildId = installation.Id,
            guildName = installation.Name,
            channels
        });
    }

    internal static async Task<IResult> PublishStandings(
        string guildId,
        string channelId,
        IGuildRepository repo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        var channel = installation.GetChannel(channelId);
        if (channel?.FollowedLeagueId is null)
        {
            return TypedResults.Ok(new { published = false, message = $"Did not publish. Channel {channelId} is not following a league." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        var gameweek = settings!.Gameweeks.GetCurrentGameweek();

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(DiscordGameweekFinishedHandler)}"));
        await endpoint.Send(new PublishStandingsToDiscordGuild(installation.Id, channel.ChannelId, (int)channel.FollowedLeagueId.Value, gameweek!.Id));

        return TypedResults.Ok(new { published = true, message = $"Published standings to {channelId}" });
    }

    internal static async Task<IResult> UpdateChannelSubscriptions(
        string guildId,
        string channelId,
        UpdateGuildChannelSubscriptionsRequest request,
        IGuildRepository repo)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        var wanted = request.Subscriptions.Select(ToFplEvent).ToHashSet();
        var current = installation.GetChannel(channelId)?.Events.Current.ToHashSet() ?? [];

        installation.Unsubscribe(channelId, current.Except(wanted).ToArray());
        installation.Subscribe(channelId, wanted.Except(current).ToArray());

        await repo.Save(installation);
        return TypedResults.Ok(new { message = $"Updated subscriptions for {channelId}" });
    }

    internal static async Task<IResult> MoveChannel(
        string guildId,
        string channelId,
        MoveGuildChannelRequest request,
        IGuildRepository repo)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        installation.MoveChannel(channelId, request.NewChannelId);
        await repo.Save(installation);

        return TypedResults.Ok(new { message = $"Moved subscription from {channelId} to {request.NewChannelId}" });
    }

    internal static async Task<IResult> DeleteSubscription(string guildId, string channelId, IGuildRepository repo)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation is not null)
        {
            installation.RemoveChannel(channelId);
            await repo.Save(installation);
        }
        return TypedResults.Ok(new { message = $"Deleted sub {guildId}-{channelId}" });
    }

    internal static async Task<IResult> DeleteAllSubscriptionsForGuild(string guildId, IGuildRepository repo)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        var count = installation?.ChannelSubscriptions.Count ?? 0;
        if (installation is not null)
        {
            foreach (var channel in installation.ChannelSubscriptions.ToList())
            {
                installation.RemoveChannel(channel.ChannelId);
            }
            await repo.Save(installation);
        }
        return TypedResults.Ok(new { message = $"Deleted {count} subscription(s) for guild {guildId}" });
    }

    // Removes the guild itself and every channel subscription under it — the same cleanup
    // GuildStatusChecker already does when it discovers a guild is no longer reachable, just
    // triggered manually from the admin UI instead of automatically. This only forgets our
    // own tracked data; it doesn't call Discord to remove the bot from the server (there's no
    // "leave guild" support in DiscordClient today).
    internal static async Task<IResult> DeleteGuild(string guildId, IGuildRepository repo)
    {
        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation is not null)
        {
            await repo.Delete(installation);
        }
        return TypedResults.Ok(new { message = $"Deleted guild {guildId}" });
    }
}
