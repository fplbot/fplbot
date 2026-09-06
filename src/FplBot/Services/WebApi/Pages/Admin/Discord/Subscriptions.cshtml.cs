using FplBot.Data.Discord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.WebApi.Pages.Admin.Discord;

public class Subscriptions(IGuildRepository repo, IConnectionMultiplexer plexer, IOptions<RedisOptions> options)
    : PageModel
{
    private readonly IServer _server = plexer.GetServer(options.Value.GetRedisServerHostAndPort);
    private readonly IDatabase _db = plexer.GetDatabase();

    public async Task OnGet()
    {
        var guilds = await repo.GetAllGuilds();

        GuildsWithSubs = new List<GuildWithSubs>();
        var allsubs = await repo.GetAllGuildSubscriptions();
        foreach (var guild in guilds)
        {
            var subs = allsubs.Where(s => s.GuildId == guild.Id);
            GuildsWithSubs.Add(new GuildWithSubs(guild, subs));
        }
    }

    public List<GuildWithSubs> GuildsWithSubs { get; set; } = null!;

    public async Task<IActionResult> OnPostDeleteKey(string key)
    {
        await _db.KeyDeleteAsync(key);
        TempData["msg"] = $"Deleted {key}";
        return RedirectToPage("Subscriptions");
    }

    public async Task<IActionResult> OnPostDeleteSub(string guildId, string channelId)
    {
        await repo.DeleteGuildSubscription(guildId, channelId);
        TempData["msg"] = $"Deleted sub {guildId}-{channelId}";
        return RedirectToPage("Subscriptions");
    }
}

public record GuildWithSubs(GuildRepoGuild guild, IEnumerable<GuildFplSubscription> Subs);
