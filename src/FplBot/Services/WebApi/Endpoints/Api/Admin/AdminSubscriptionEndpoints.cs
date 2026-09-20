using Fpl.Client.Abstractions;
using Fpl.PulseLive;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Api.Admin;

// The events an admin can trigger on demand for one channel or subscriber, right now, without
// waiting for the real trigger. Each reuses the exact command and current-gameweek assumption the
// real event-driven flow already uses — nothing here is a distinct code path. The real system only
// ever sends one of two fixed deadline reminders (24 hours out, 1 hour out), so both are exposed
// separately rather than collapsed into one invented "deadline" message that never actually occurs.
public enum PublishableEvent
{
    Standings,
    GameweekStarted,
    Deadline24Hours,
    Deadline1Hour,
    FixtureEvents,
    FixtureFullTime,
    Lineups
}

// A subscription id is unique across platforms and already says which platform it belongs to, so
// the admin UI addresses one the same way whether it lives in Slack or Discord. Only the
// implementation behind it differs.
public static class AdminSubscriptionEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/subscriptions/{subscriptionId}", GetInstallation);
        group.MapPut("/subscriptions/{subscriptionId}/subscriptions", UpdateSubscriptions);
        group.MapPut("/subscriptions/{subscriptionId}/channel", MoveChannel);
        group.MapPut("/subscriptions/{subscriptionId}/league", FollowLeague);
        group.MapDelete("/subscriptions/{subscriptionId}/league", UnfollowLeague);
        group.MapDelete("/subscriptions/{subscriptionId}", Delete);
        group.MapPost("/subscriptions/{subscriptionId}/publish/{eventName}", Publish);
    }

    private static async Task<ChatPlatform?> PlatformOf(IIdentityResolver resolver, string subscriptionId) =>
        (await resolver.ResolveSubscription(new SubscriptionId(subscriptionId)))?.Platform;

    internal static async Task<IResult> GetInstallation(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.GetSubscriptionInstallation(subscriptionId, resolver, slackRepo),
            ChatPlatform.Discord => await AdminDiscordEndpoints.GetSubscriptionInstallation(subscriptionId, resolver, guildRepo),
            _ => TypedResults.NotFound()
        };

    internal static async Task<IResult> UpdateSubscriptions(
        string subscriptionId,
        UpdateChannelSubscriptionsRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.UpdateChannelSubscriptions(subscriptionId, request, resolver, slackRepo),
            ChatPlatform.Discord => await AdminDiscordEndpoints.UpdateChannelSubscriptions(subscriptionId, request, resolver, guildRepo),
            _ => TypedResults.NotFound()
        };

    internal static async Task<IResult> MoveChannel(
        string subscriptionId,
        MoveChannelRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo,
        IPublishEndpoint publishEndpoint) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.MoveChannel(subscriptionId, request, resolver, slackRepo, publishEndpoint),
            ChatPlatform.Discord => await AdminDiscordEndpoints.MoveChannel(subscriptionId, request, resolver, guildRepo, publishEndpoint),
            _ => TypedResults.NotFound()
        };

    internal static async Task<IResult> FollowLeague(
        string subscriptionId,
        FollowLeagueRequest request,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo,
        ILeagueClient leagueClient) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.FollowLeague(subscriptionId, request, resolver, slackRepo, leagueClient),
            ChatPlatform.Discord => await AdminDiscordEndpoints.FollowLeague(subscriptionId, request, resolver, guildRepo, leagueClient),
            _ => TypedResults.NotFound()
        };

    internal static async Task<IResult> UnfollowLeague(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.UnfollowLeague(subscriptionId, resolver, slackRepo),
            ChatPlatform.Discord => await AdminDiscordEndpoints.UnfollowLeague(subscriptionId, resolver, guildRepo),
            _ => TypedResults.NotFound()
        };

    internal static async Task<IResult> Delete(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.DeleteChannelSubscription(subscriptionId, resolver, slackRepo),
            ChatPlatform.Discord => await AdminDiscordEndpoints.DeleteSubscription(subscriptionId, resolver, guildRepo),
            _ => TypedResults.NotFound()
        };

    internal static async Task<IResult> Publish(
        string subscriptionId,
        string eventName,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient,
        IFixtureClient fixtureClient,
        ILiveClient liveClient,
        IPulseLiveClient pulseClient)
    {
        if (!Enum.TryParse<PublishableEvent>(eventName, ignoreCase: true, out var evt))
        {
            return TypedResults.BadRequest(new { errors = new { eventName = new[] { "must be one of Standings, GameweekStarted, Deadline24Hours, Deadline1Hour, FixtureEvents, FixtureFullTime, Lineups" } } });
        }

        return await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.Publish(subscriptionId, evt, resolver, slackRepo, sendEndpointProvider, gameweekClient,
                fixtureClient, liveClient, pulseClient),
            ChatPlatform.Discord => await AdminDiscordEndpoints.Publish(subscriptionId, evt, resolver, guildRepo, sendEndpointProvider, gameweekClient,
                fixtureClient, liveClient, pulseClient),
            _ => TypedResults.NotFound()
        };
    }
}
