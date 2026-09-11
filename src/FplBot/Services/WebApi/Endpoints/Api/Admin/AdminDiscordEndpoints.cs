using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using FplBot.Discord;
using Microsoft.Extensions.Caching.Memory;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record GuildWithSubsDto(string GuildId, string GuildName, IEnumerable<GuildFplSubscription> Subscriptions);

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
    }

    private static async Task<IResult> GetSlashCommands(DiscordSlashCommandsEnsurer ensurer)
    {
        var commands = await ensurer.GetAllForGuild(TestGuildId);
        return TypedResults.Ok(commands);
    }

    private static IResult GetSlashCommandDefinitions()
    {
        return TypedResults.Ok(DiscordSlashCommandsEnsurer.GetDefinedCommandSummaries());
    }

    private static async Task<IResult> InstallSlashCommands(DiscordSlashCommandsEnsurer ensurer)
    {
        await ensurer.InstallGuildSlashCommandsInGuild(TestGuildId);
        return TypedResults.Ok(new { message = "Install queued!" });
    }

    private static async Task<IResult> InstallGlobalSlashCommands(DiscordSlashCommandsEnsurer ensurer)
    {
        await ensurer.InstallGuildSlashCommandsInGuild();
        return TypedResults.Ok(new { message = "Global install queued!" });
    }

    private static async Task<IResult> UninstallSlashCommands(DiscordSlashCommandsEnsurer ensurer)
    {
        await ensurer.DeleteGuildSlashCommands(TestGuildId);
        return TypedResults.Ok(new { message = "Uninstall queued!" });
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

    private static async Task<IResult> DeleteSubscription(string guildId, string channelId, IGuildRepository repo)
    {
        await repo.DeleteGuildSubscription(guildId, channelId);
        return TypedResults.Ok(new { message = $"Deleted sub {guildId}-{channelId}" });
    }
}
