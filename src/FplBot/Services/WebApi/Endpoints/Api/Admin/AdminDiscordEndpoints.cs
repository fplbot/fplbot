using Discord.Net.HttpClients;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using Fpl.PulseLive;
using FplBot.Data;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Discord;
using FplBot.Domain;
using FplBot.EventHandlers.Discord;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record GuildWithSubsDto(string Id, string GuildId, string GuildName, IEnumerable<ChannelSubscriptionDto> Subscriptions);

// TotalApproximateMembers mirrors Discord's own "approximate_member_count" field name and
// semantics: an approximation that includes bot accounts, not a unique human count. Never
// relabel this "users" or "unique users" downstream.
public record GuildReachStatsDto(int TotalGuilds, long TotalApproximateMembers);

public record DiscordBroadcastRequest(string Message, ChannelFilter Filter);

public static class AdminDiscordEndpoints
{
    // Slash commands are only ever managed for this one hardcoded test guild today —
    // carried over unchanged from Pages/Admin/Discord/Slashcommands.cshtml.cs.
    // A subscription id is enough to address a subscription, but the admin UI still needs its
    // installation to render the guild around it.
    internal static async Task<IResult> GetSubscriptionInstallation(
        string subscriptionId,
        IIdentityResolver resolver,
        IGuildRepository repo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();

        var installation = await repo.FindInstallationByTeamId(resolved.GuildId);
        return installation is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new { installationId = installation.Id.Value, platform = nameof(ChatPlatform.Discord) });
    }

    private static async Task<string?> ResolveGuildId(IIdentityResolver resolver, string installationId) =>
        await resolver.ResolveInstallation(new InstallationId(installationId)) is { Platform: ChatPlatform.Discord } installation
            ? installation.ExternalId
            : null;

    private static async Task<(string GuildId, string ChannelId)?> ResolveChannel(IIdentityResolver resolver, string subscriptionId) =>
        await resolver.ResolveSubscription(new SubscriptionId(subscriptionId)) is { Platform: ChatPlatform.Discord } subscription
            ? (subscription.InstallationExternalId, subscription.ChannelId)
            : null;

    private const string TestGuildId = "893932860162064414";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/discord/slashcommands", GetSlashCommands);
        group.MapGet("/discord/slashcommands/definitions", GetSlashCommandDefinitions);
        group.MapPost("/discord/slashcommands/install", InstallSlashCommands);
        group.MapPost("/discord/slashcommands/install-global", InstallGlobalSlashCommands);
        group.MapPost("/discord/slashcommands/uninstall", UninstallSlashCommands);

        group.MapGet("/discord/servers", GetSubscriptions);
        group.MapGet("/discord/reach", GetReachStats);
        group.MapDelete("/discord/guilds/{installationId}/subscriptions", DeleteAllSubscriptionsForGuild);
        group.MapDelete("/discord/guilds/{installationId}", DeleteGuild);

        group.MapGet("/discord/guilds/{installationId}", GetGuild);
        group.MapGet("/discord/guilds/{installationId}/available-channels", GetAvailableChannels);
        group.MapPost("/discord/guilds/{installationId}/channels", AddChannel);

        group.MapGet("/discord/failures", GetFailureStats);
        group.MapPost("/discord/failures/reset", ResetFailures);

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

    internal static async Task<IResult> GetFailureStats(IGuildRepository repo) =>
        TypedResults.Ok(await ChannelFailureAdmin.GetStats(repo, DateTimeOffset.UtcNow));

    internal static async Task<IResult> ResetFailures(IGuildRepository repo, ILogger<Program> logger)
    {
        var cleared = await ChannelFailureAdmin.ResetAll(repo, logger);
        logger.LogWarning("Admin reset delivery failure counters for {Cleared} Discord channel(s)", cleared);
        return TypedResults.Ok(new { cleared });
    }

    internal static async Task<IResult> GetSubscriptions(string? query, int? page, int? pageSize, bool? failingOnly, IGuildRepository repo)
    {
        var pageNumber = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, 100) : 25;

        var installations = (await repo.GetAllInstallations()).ToList();

        var guildsWithSubs = installations
            .Select(i => new GuildWithSubsDto(i.Id.Value, i.ExternalId, i.Name, i.ChannelSubscriptions.Select(c => ToDto(i.ExternalId, c))))
            .ToList();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? guildsWithSubs
            :
            [
                .. guildsWithSubs.Where(g =>
                    g.GuildName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    g.GuildId.Contains(query, StringComparison.OrdinalIgnoreCase))
            ];

        if (failingOnly is true)
        {
            filtered = [.. filtered.Where(g => g.Subscriptions.Any(c => c.FailureCount > 0))];
        }

        var items = filtered
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToList();

        return TypedResults.Ok(new PagedResult<GuildWithSubsDto>(items, pageNumber, size, filtered.Count));
    }

    // The future admin analytics page's total-reach number: totalGuilds is every installed
    // guild, totalApproximateMembers sums whatever GuildStatusChecker's hourly sweep has
    // fetched so far - a guild not yet swept (or perpetually Forbidden) simply contributes 0.
    internal static async Task<IResult> GetReachStats(IGuildRepository repo, IGuildMemberCountRepository memberCountRepo)
    {
        var totalGuilds = (await repo.GetAllInstallations()).Count();
        var totalApproximateMembers = (await memberCountRepo.GetAll()).Values.Sum(v => (long)v);
        return TypedResults.Ok(new GuildReachStatsDto(totalGuilds, totalApproximateMembers));
    }

    private static ChannelSubscriptionDto ToDto(string guildId, ChannelSubscription channel) =>
        new(channel.Id.Value, guildId, channel.ChannelId, channel.FollowedLeagueId is { } id ? (int)id.Value : null,
            ToEventSubscriptions(channel), channel.FailureCount, channel.FailingSince, channel.LastFailureReason);

    private static IEnumerable<EventSubscription> ToEventSubscriptions(ChannelSubscription? channel) =>
        channel?.Events.Current.Select(e => Enum.Parse<EventSubscription>(e.ToString())) ?? [];

    private static FplEvent ToFplEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());

    internal static async Task<IResult> GetAvailableChannels(
        string installationId,
        IIdentityResolver resolver,
        IGuildRepository repo,
        IDiscordClient discordClient,
        ILogger<Program> logger)
    {
        if (await ResolveGuildId(resolver, installationId) is not { } guildId) return TypedResults.NotFound();

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        var namesByChannelId = await ListTextChannels(guildId, discordClient, logger);
        if (namesByChannelId is null)
        {
            return TypedResults.Problem(
                title: "Failed to list channels from Discord",
                detail: "The Discord guild channels call failed. Enter a channel id manually instead.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return TypedResults.Ok(namesByChannelId.Select(c => new ChannelDto(c.Key, c.Value)).OrderBy(c => c.Name));
    }

    // Only text (0) and announcement (5) channels can receive a notification — a guild's
    // categories and voice channels are not valid move targets.
    private static async Task<Dictionary<string, string>?> ListTextChannels(
        string guildId,
        IDiscordClient discordClient,
        ILogger logger)
    {
        try
        {
            var guildChannels = await discordClient.GuildChannelsGet(guildId);
            return guildChannels
                .Where(c => c.Type is 0 or 5)
                .ToDictionary(c => c.Id.ToString(), c => c.Name);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return null;
        }
    }

    internal static async Task<IResult> GetGuild(
        string installationId,
        IIdentityResolver resolver,
        IGuildRepository repo,
        ILeagueClient leagueClient,
        IDiscordClient discordClient,
        ILogger<Program> logger)
    {
        if (await ResolveGuildId(resolver, installationId) is not { } guildId) return TypedResults.NotFound();

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        var namesByChannelId = await ListTextChannels(guildId, discordClient, logger);

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

            var channelStatus = namesByChannelId?.ContainsKey(channel.ChannelId);
            var channelName = namesByChannelId?.GetValueOrDefault(channel.ChannelId);

            channels.Add(new
            {
                id = channel.Id.Value,
                channel = channel.ChannelId,
                channelName,
                leagueId,
                leagueName,
                subscriptions = ToEventSubscriptions(channel),
                channelStatus,
                failureCount = channel.FailureCount,
                failingSince = channel.FailingSince,
                lastFailureReason = channel.LastFailureReason,
                purgeEligibleAt = channel.FailingSince is { } since ? since + ChannelSubscription.MaxFailureAge : (DateTimeOffset?)null,
                failuresUntilPurge = Math.Max(0, ChannelSubscription.MaxFailures - channel.FailureCount),
                purgeFailureLimit = ChannelSubscription.MaxFailures
            });
        }

        return TypedResults.Ok(new { id = installation.Id.Value, guildId = installation.ExternalId, guildName = installation.Name, channels });
    }

    internal static async Task<IResult> Publish(
        string subscriptionId,
        PublishableEvent evt,
        IIdentityResolver resolver,
        IGuildRepository repo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient,
        IFixtureClient fixtureClient,
        ILiveClient liveClient,
        IPulseLiveClient pulseClient)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (guildId, channelId) = resolved;

        var installation = await repo.FindInstallationByTeamId(guildId);
        var channel = installation?.GetChannel(channelId);
        if (installation is null || channel is null) return TypedResults.NotFound();

        var requiresLeague = evt is PublishableEvent.Standings or PublishableEvent.GameweekStarted;
        if (requiresLeague && channel.FollowedLeagueId is null)
        {
            return TypedResults.Ok(new { published = false, message = $"Did not publish. Channel {channelId} is not following a league." });
        }

        var settings = await gameweekClient.GetGlobalSettings();
        if (settings?.Gameweeks.GetCurrentGameweek() is not { } gameweek)
        {
            return TypedResults.Ok(new { published = false, message = "Could not determine the current gameweek." });
        }

        switch (evt)
        {
            case PublishableEvent.Standings:
                var standingsEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(DiscordGameweekFinishedHandler)}"));
                await standingsEndpoint.Send(new PublishStandingsToDiscordGuild(installation.ExternalId, channel.ChannelId,
                    (int)channel.FollowedLeagueId!.Value, gameweek.Id));
                break;
            case PublishableEvent.GameweekStarted:
                var startedEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(DiscordGameweekStartedHandler)}"));
                await startedEndpoint.Send(new ProcessGameweekStartedForGuildChannel(installation.ExternalId, channel.ChannelId, gameweek.Id));
                break;
            case PublishableEvent.Deadline24Hours:
                var deadline24Endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(PublishToGuildHandler)}"));
                await deadline24Endpoint.Send(new PublishToGuildChannel(installation.ExternalId, channel.ChannelId,
                    $"⏳Gameweek {gameweek.Id} deadline in 24 hours!"));
                break;
            case PublishableEvent.Deadline1Hour:
                var deadline1Endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(PublishToGuildHandler)}"));
                await deadline1Endpoint.Send(new PublishToGuildChannel(installation.ExternalId, channel.ChannelId,
                    $"😱 Gameweek {gameweek.Id} deadline in 60 minutes! @here"));
                break;
            case PublishableEvent.FixtureEvents:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                var (events, refusalReason) = await PublishableFixtureLookup.ResolveFixtureEvents(fixture, gameweekClient);
                if (refusalReason is not null)
                {
                    return TypedResults.Ok(new { published = false, message = refusalReason });
                }

                var fixtureEventsEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(DiscordFixtureEventsHandler)}"));
                await fixtureEventsEndpoint.Send(new PublishFixtureEventsToGuild(installation.ExternalId, channel.ChannelId, events));
                break;
            }
            case PublishableEvent.FixtureFullTime:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                if (!fixture.Finished && !fixture.FinishedProvisional)
                {
                    return TypedResults.Ok(new { published = false, message = "This fixture hasn't finished yet." });
                }

                var fulltimeSettings = await gameweekClient.GetGlobalSettings();
                var liveItems = fixture.Event.HasValue ? await liveClient.GetLiveItems(fixture.Event.Value, isOngoingGameweek: true) : null;
                var finished = FixtureFulltimeModelBuilder.CreateFinishedFixture(fulltimeSettings?.Teams ?? [], fulltimeSettings?.Players ?? [], fixture, liveItems);
                var title = $"*FT: {finished.HomeTeam.ShortName} {finished.Fixture.HomeTeamScore}-{finished.Fixture.AwayTeamScore} {finished.AwayTeam.ShortName}*";
                var threadMessage = Formatter.FormatProvisionalFinished(finished);

                var fulltimeEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(PublishToGuildHandler)}"));
                await fulltimeEndpoint.Send(new PublishRichToGuildChannel(installation.ExternalId, channel.ChannelId, $"ℹ️ {title}", threadMessage));
                break;
            }
            case PublishableEvent.Lineups:
            {
                if (await PublishableFixtureLookup.FindFirstFixture(fixtureClient, gameweek.Id) is not { } fixture)
                {
                    return TypedResults.Ok(new { published = false, message = "No fixtures found for the current gameweek." });
                }

                var matchDetails = await pulseClient.GetMatchDetails(fixture.Code);
                if (matchDetails is null || !matchDetails.HasLineUps())
                {
                    return TypedResults.Ok(new { published = false, message = "Lineups aren't confirmed yet for this fixture." });
                }

                var lineupSettings = await gameweekClient.GetGlobalSettings();
                var teamShortNames = lineupSettings?.Teams.ToDictionary(t => t.Id, t => t.ShortName) ?? [];
                var homeAbbr = teamShortNames.GetValueOrDefault(fixture.HomeTeamId, "?") ?? "?";
                var awayAbbr = teamShortNames.GetValueOrDefault(fixture.AwayTeamId, "?") ?? "?";
                var lineupReady = MatchDetailsMapper.TryMapToLineup(matchDetails, fixture.Code, homeAbbr, awayAbbr);
                if (lineupReady is null)
                {
                    return TypedResults.Ok(new { published = false, message = "Could not map lineups for this fixture." });
                }

                var firstMessage = $"*Lineups {lineupReady.Lineup.HomeTeamLineup.TeamName}-{lineupReady.Lineup.AwayTeamLineup.TeamName} ready* ";
                var formattedLineup = Formatter.FormatLineup(lineupReady.Lineup);
                var lineupsEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(PublishToGuildHandler)}"));
                await lineupsEndpoint.Send(new PublishRichToGuildChannel(installation.ExternalId, channel.ChannelId, $"ℹ️ {firstMessage}", formattedLineup));
                break;
            }
        }

        return TypedResults.Ok(new { published = true, message = $"Published {evt} to {channelId}" });
    }

    internal static async Task<IResult> UpdateChannelSubscriptions(
        string subscriptionId,
        UpdateChannelSubscriptionsRequest request,
        IIdentityResolver resolver,
        IGuildRepository repo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (guildId, channelId) = resolved;

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        var wanted = request.Subscriptions.Select(ToFplEvent).ToHashSet();
        var current = installation.GetChannel(channelId)?.Events.Current.ToHashSet() ?? [];

        installation.Unsubscribe(channelId, [.. current.Except(wanted)]);
        installation.Subscribe(channelId, [.. wanted.Except(current)]);

        await repo.Save(installation);
        return TypedResults.Ok(new { message = $"Updated subscriptions for {channelId}" });
    }

    internal static async Task<IResult> FollowLeague(
        string subscriptionId,
        FollowLeagueRequest request,
        IIdentityResolver resolver,
        IGuildRepository repo,
        ILeagueClient leagueClient)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (guildId, channelId) = resolved;

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();
        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        var league = await leagueClient.GetClassicLeague(request.LeagueId, tolerate404: true);
        if (league == null)
        {
            return TypedResults.BadRequest(new { message = $"Could not find a classic league with id '{request.LeagueId}'." });
        }

        installation.Follow(channelId, new ClassicLeagueId(request.LeagueId));
        await repo.Save(installation);

        var leagueName = league.Properties?.Name;
        return TypedResults.Ok(new { message = $"{channelId} now follows '{leagueName}' ({request.LeagueId})", leagueName });
    }

    internal static async Task<IResult> UnfollowLeague(
        string subscriptionId,
        IIdentityResolver resolver,
        IGuildRepository repo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (guildId, channelId) = resolved;

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();
        if (installation.GetChannel(channelId) is null) return TypedResults.NotFound();

        installation.Unfollow(channelId);
        await repo.Save(installation);

        return TypedResults.Ok(new { message = $"{channelId} no longer follows a league" });
    }

    internal static async Task<IResult> AddChannel(
        string installationId,
        AddChannelRequest request,
        IIdentityResolver resolver,
        IGuildRepository repo)
    {
        if (await ResolveGuildId(resolver, installationId) is not { } guildId) return TypedResults.NotFound();

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        if (string.IsNullOrWhiteSpace(request.ChannelId))
        {
            return TypedResults.BadRequest(new { message = "A channel id is required." });
        }

        if (installation.GetChannel(request.ChannelId) is not null)
        {
            return TypedResults.Conflict(new { message = $"{request.ChannelId} already has a subscription." });
        }

        installation.Subscribe(request.ChannelId, [FplEvent.All]);
        await repo.Save(installation);

        return TypedResults.Ok(new { message = $"Subscribed {request.ChannelId} to all events" });
    }

    internal static async Task<IResult> MoveChannel(
        string subscriptionId,
        MoveChannelRequest request,
        IIdentityResolver resolver,
        IGuildRepository repo,
        IPublishEndpoint publishEndpoint)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (guildId, channelId) = resolved;

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation == null) return TypedResults.NotFound();

        var moveChannelOutcome = installation.MoveChannel(channelId, request.NewChannelId);
        IResult result = moveChannelOutcome switch
        {
            MoveChannelOutcome.SourceNotFound => TypedResults.NotFound(),
            MoveChannelOutcome.TargetAlreadySubscribed => TypedResults.Conflict(new { message = $"{request.NewChannelId} already has a subscription." }),
            MoveChannelOutcome.Moved => TypedResults.Ok(new { message = $"Moved subscription from {channelId} to {request.NewChannelId}" }),
            _ => throw new ArgumentOutOfRangeException(nameof(moveChannelOutcome), moveChannelOutcome, null)
        };

        if (moveChannelOutcome is not MoveChannelOutcome.Moved)
        {
            return result;
        }

        await repo.Save(installation);

        await publishEndpoint.Publish(new DiscordChannelMoved(guildId, channelId, request.NewChannelId));

        return result;
    }

    internal static async Task<IResult> DeleteSubscription(string subscriptionId, IIdentityResolver resolver, IGuildRepository repo)
    {
        if (await ResolveChannel(resolver, subscriptionId) is not { } resolved) return TypedResults.NotFound();
        var (guildId, channelId) = resolved;

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation is not null)
        {
            installation.RemoveChannel(channelId);
            await repo.Save(installation);
        }

        return TypedResults.Ok(new { message = $"Deleted sub {guildId}-{channelId}" });
    }

    internal static async Task<IResult> DeleteAllSubscriptionsForGuild(string installationId, IIdentityResolver resolver, IGuildRepository repo)
    {
        if (await ResolveGuildId(resolver, installationId) is not { } guildId) return TypedResults.NotFound();

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

    // Removes the guild itself and every channel subscription under it, and takes the bot out of
    // the server on the way — an admin uninstall that only forgot our own data would leave the bot
    // sitting in the member list of a server we no longer track. Leaving is best-effort: the usual
    // reason a guild gets removed is that the bot was kicked already, which is also the case
    // GuildStatusChecker handles automatically, and Discord answers that with a 4xx.
    internal static async Task<IResult> DeleteGuild(
        string installationId,
        IIdentityResolver resolver,
        IGuildRepository repo,
        IDiscordClient discordClient,
        ILogger<Program> logger)
    {
        if (await ResolveGuildId(resolver, installationId) is not { } guildId) return TypedResults.NotFound();

        try
        {
            await discordClient.GuildLeave(guildId);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Could not leave guild {GuildId}, deleting it anyway", guildId);
        }

        var installation = await repo.FindInstallationByTeamId(guildId);
        if (installation is not null)
        {
            await repo.Delete(installation);
        }

        return TypedResults.Ok(new { message = $"Deleted guild {guildId}" });
    }
}
