using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Discord.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.WebApi.Pages.Admin.Discord
{
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
            Subs = await _repo.GetAllGuildSubscriptions();
            AllKeys = _server.Keys(pattern: "*");
        }

        public async Task<IActionResult> OnPostDeleteKey(string key)
        {
            await _db.KeyDeleteAsync(key);
            TempData["msg"] = $"Deleted {key}";
            return RedirectToPage("Subscriptions");
        }

        public IEnumerable<RedisKey> AllKeys { get; set; }

        public IEnumerable<GuildFplSubscription> Subs { get; set; }
    }
}
