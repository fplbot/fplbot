using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Search.Data.Repositories;
using FplBot.Discord.Data;
using FplBot.Slack.Data;
using FplBot.Slack.Data.Repositories.Redis;
using FplBot.VerifiedEntries.Data;
using FplBot.VerifiedEntries.Data.Repositories;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.WebApi.Tests
{
    public class GuildRepositoryTests
    {
        private readonly ITestOutputHelper _helper;
        private readonly IServer _server;
        private readonly DiscordGuildStore
            _repo;

        public GuildRepositoryTests(ITestOutputHelper helper)
        {
            _helper = helper;
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
            _server = multiplexer.GetServer(opts.Value.GetRedisServerHostAndPort);
            _repo = new DiscordGuildStore(multiplexer, opts, new SimpleLogger(_helper));

        }

        [Fact]
        public async Task Insert()
        {
            await _repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, new[] { EventSubscription.All }));

            var guildSub = await _repo.GetGuildSubscription("Guild1", "Channel1");
            Assert.NotNull(guildSub);
            Assert.NotEmpty(guildSub.Subscriptions);
        }

        [Fact]
        public async Task GetMany()
        {
            await _repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, new[] { EventSubscription.All }));
            await _repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel2", null, new[] { EventSubscription.Standings }));

            var subs = await _repo.GetAllSubscriptionInGuild("Guild1");

            Assert.Equal(2, subs.Count());

            var sub1 = await _repo.GetGuildSubscription("Guild1", "Channel1");
            var sub2 = await _repo.GetGuildSubscription("Guild1", "Channel2");

            Assert.Equal(EventSubscription.All, sub1.Subscriptions.First());
            Assert.Equal(EventSubscription.Standings, sub2.Subscriptions.First());

            var all = await _repo.GetAllGuildSubscriptions();
            Assert.Equal(2, all.Count());
        }
    }
}
