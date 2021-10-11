using System;
using System.Linq;
using System.Threading.Tasks;
using FplBot.Discord.Data;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.WebApi.Tests
{
    public class GuildRepoTests : IDisposable
    {
        private readonly DiscordGuildStore _repo;
        private readonly IServer _server;

        public GuildRepoTests(ITestOutputHelper helper)
        {
            (_server, _repo) = Factory.CreateRepo(helper);
        }

        [Fact]
        public async Task Insert_Works()
        {
            await _repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, new[] { EventSubscription.All }));

            var guildSub = await _repo.GetGuildSubscription("Guild1", "Channel1");
            Assert.NotNull(guildSub);
            Assert.NotEmpty(guildSub.Subscriptions);
        }

        [Fact]
        public async Task GetMany_Works()
        {
            await _repo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel1", null, new[] { EventSubscription.All }));
            await _repo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel2", null, new[] { EventSubscription.Standings }));

            var subs = await _repo.GetAllSubscriptionInGuild("Guild2");

            Assert.Equal(2, subs.Count());

            var sub1 = await _repo.GetGuildSubscription("Guild2", "Channel1");
            var sub2 = await _repo.GetGuildSubscription("Guild2", "Channel2");

            Assert.Equal(EventSubscription.All, sub1.Subscriptions.First());
            Assert.Equal(EventSubscription.Standings, sub2.Subscriptions.First());

            var all = await _repo.GetAllGuildSubscriptions();
            Assert.Equal(2, all.Count());
        }

        public void Dispose()
        {
            _server.FlushDatabase();
        }
    }

    public class Factory
    {
        public static (IServer, DiscordGuildStore) CreateRepo(ITestOutputHelper helper)
        {
            var opts = new OptionsWrapper<DiscordRedisOptions>(new DiscordRedisOptions
            {
                REDIS_URL = Environment.GetEnvironmentVariable("HEROKU_REDIS_COPPER_URL"),
            });
            var configurationOptions = new ConfigurationOptions
            {
                ClientName = opts.Value.GetRedisUsername,
                Password = opts.Value.GetRedisPassword,
                EndPoints = { opts.Value.GetRedisServerHostAndPort },
                AllowAdmin = true
            };

            var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            var server = multiplexer.GetServer(opts.Value.GetRedisServerHostAndPort);
            var repo = new DiscordGuildStore(multiplexer, opts, new SimpleLogger(helper));
            return (server, repo);
        }
    }
}
