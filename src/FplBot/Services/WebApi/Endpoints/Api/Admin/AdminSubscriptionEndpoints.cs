using Fpl.Client.Abstractions;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Api.Admin;

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
        group.MapPost("/subscriptions/{subscriptionId}/publish-standings", PublishStandings);
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

    internal static async Task<IResult> PublishStandings(
        string subscriptionId,
        IIdentityResolver resolver,
        ISlackTeamRepository slackRepo,
        IGuildRepository guildRepo,
        ISendEndpointProvider sendEndpointProvider,
        IGlobalSettingsClient gameweekClient) =>
        await PlatformOf(resolver, subscriptionId) switch
        {
            ChatPlatform.Slack => await AdminSlackEndpoints.PublishStandings(subscriptionId, resolver, slackRepo, sendEndpointProvider, gameweekClient),
            ChatPlatform.Discord => await AdminDiscordEndpoints.PublishStandings(subscriptionId, resolver, guildRepo, sendEndpointProvider, gameweekClient),
            _ => TypedResults.NotFound()
        };
}
