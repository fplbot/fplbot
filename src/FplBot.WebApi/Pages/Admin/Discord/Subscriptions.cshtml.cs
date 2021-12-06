using Discord.Net.Endpoints.Hosting;
using FplBot.Discord.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.WebApi.Pages.Admin.Discord;

public class Subscriptions : PageModel
{
    private readonly IGuildRepository _repo;
    private readonly IServer _server;
    private readonly IDatabase _db;

    public Subscriptions(IGuildRepository repo, IConnectionMultiplexer plexer, IOptions<DiscordRedisOptions> options)
    {
        _repo = repo;
        _db = plexer.GetDatabase();
        _server = plexer.GetServer(options.Value.GetRedisServerHostAndPort);
    }

    public async Task OnGet()
    {
        var guilds = await _repo.GetAllGuilds();

        GuildsWithSubs = new List<GuildWithSubs>();
        var allsubs = await _repo.GetAllGuildSubscriptions();
        foreach (var guild in guilds)
        {
            var subs = allsubs.Where(s => s.GuildId == guild.Id);
            GuildsWithSubs.Add(new GuildWithSubs(guild, subs));
        }
    }

    public List<GuildWithSubs> GuildsWithSubs { get; set; }

    public async Task<IActionResult> OnPostDeleteKey(string key)
    {
        await _db.KeyDeleteAsync(key);
        TempData["msg"] = $"Deleted {key}";
        return RedirectToPage("Subscriptions");
    }

    public async Task<IActionResult> OnPostDeleteSub(string guildId, string channelId)
    {
        await _repo.DeleteGuildSubscription(guildId, channelId);
        TempData["msg"] = $"Deleted sub {guildId}-{channelId}";
        return RedirectToPage("Subscriptions");
    }
}

public record GuildWithSubs(Guild guild, IEnumerable<GuildFplSubscription> Subs);
