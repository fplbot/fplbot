using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using FplBot.Discord;
using FplBot.EventHandlers.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record GuildWithSubsDto(string GuildId, string GuildName, IEnumerable<GuildFplSubscription> Subscriptions);

public record DiscordBroadcastRequest(string Message, ChannelFilter Filter);

public static class AdminDiscordEndpoints
{
    // Slash commands are only ever managed for this one hardcoded test guild today —
    // carried over unchanged from Pages/Admin/Discord/Slashcommands.cshtml.cs.
    private const string TestGuildId = "893932860162064414";

    private const string GuildsCacheKey = "admin:discord:guilds";
    private const string GuildSubsCacheKey = "admin:discord:guild-subs";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/discord/slashcommands", GetSlashCommands);
        group.MapGet("/discord/slashcommands/definitions", GetSlashCommandDefinitions);
        group.MapPost("/discord/slashcommands/install", InstallSlashCommands);
        group.MapPost("/discord/slashcommands/install-global", InstallGlobalSlashCommands);
        group.MapPost("/discord/slashcommands/uninstall", UninstallSlashCommands);

        group.MapGet("/discord/subscriptions", GetSubscriptions);
        group.MapDelete("/discord/subscriptions/{guildId}/{channelId}", DeleteSubscription);
        group.MapDelete("/discord/guilds/{guildId}", DeleteGuild);

        group.MapPost("/discord/broadcast", BroadcastToDiscord);
    }

    private static async Task<IResult> BroadcastToDiscord(DiscordBroadcastRequest request, ISendEndpointProvider sendEndpointProvider, ILogger<Program> logger)
    {
        logger.LogInformation("ENQUEUEING BROADCAST TO DISCORD");
        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(BroadcastHandler)}"));
            await endpoint.Send(new FplBot.Messaging.Contracts.Commands.v1.BroadcastToDiscord(request.Message, request.Filter));
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

    private static async Task<IResult> GetSubscriptions(string? query, int page, int pageSize, IGuildRepository repo, IMemoryCache cache)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);

        var guilds = (await cache.GetOrCreateAsync(GuildsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (await repo.GetAllGuilds()).ToList();
        }))!;

        var allSubs = (await cache.GetOrCreateAsync(GuildSubsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (await repo.GetAllGuildSubscriptions()).ToList();
        }))!;

        var guildsWithSubs = guilds
            .Select(g => new GuildWithSubsDto(g.Id, g.Name, allSubs.Where(s => s.GuildId == g.Id)))
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

    private static async Task<IResult> DeleteSubscription(string guildId, string channelId, IGuildRepository repo, IMemoryCache cache)
    {
        await repo.DeleteGuildSubscription(guildId, channelId);
        InvalidateGuildCaches(cache);
        return TypedResults.Ok(new { message = $"Deleted sub {guildId}-{channelId}" });
    }

    // Removes the guild itself and every channel subscription under it — the same cleanup
    // GuildStatusChecker already does when it discovers a guild is no longer reachable, just
    // triggered manually from the admin UI instead of automatically. This only forgets our
    // own tracked data; it doesn't call Discord to remove the bot from the server (there's no
    // "leave guild" support in DiscordClient today).
    private static async Task<IResult> DeleteGuild(string guildId, IGuildRepository repo, IMemoryCache cache)
    {
        var subs = (await repo.GetAllGuildSubscriptions()).Where(s => s.GuildId == guildId);
        foreach (var sub in subs)
        {
            await repo.DeleteGuildSubscription(sub.GuildId, sub.ChannelId);
        }
        await repo.DeleteGuild(guildId);
        InvalidateGuildCaches(cache);
        return TypedResults.Ok(new { message = $"Deleted guild {guildId}" });
    }

    private static void InvalidateGuildCaches(IMemoryCache cache)
    {
        cache.Remove(GuildsCacheKey);
        cache.Remove(GuildSubsCacheKey);
    }
}
