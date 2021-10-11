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
    public class GuildRepositoryTests : IDisposable
    {
        private readonly DiscordGuildStore Repo;
        private readonly IServer Server;

        public GuildRepositoryTests(ITestOutputHelper helper)
        {
            (Server, Repo) = Factory.CreateRepo(helper);
            Server.FlushDatabase();
        }

        [Fact]
        public async Task Insert_Works()
        {
            await Repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, new[] { EventSubscription.All }));

            var guildSub = await Repo.GetGuildSubscription("Guild1", "Channel1");
            Assert.NotNull(guildSub);
            Assert.NotEmpty(guildSub.Subscriptions);
        }

        [Fact]
        public async Task GetMany_Works()
        {
            await Repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, new[] { EventSubscription.All }));
            await Repo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel2", null, new[] { EventSubscription.Standings }));

            var subs = await Repo.GetAllSubscriptionInGuild("Guild1");

            Assert.Equal(2, subs.Count());

            var sub1 = await Repo.GetGuildSubscription("Guild1", "Channel1");
            var sub2 = await Repo.GetGuildSubscription("Guild1", "Channel2");

            Assert.Equal(EventSubscription.All, sub1.Subscriptions.First());
            Assert.Equal(EventSubscription.Standings, sub2.Subscriptions.First());

            var all = await Repo.GetAllGuildSubscriptions();
            Assert.Equal(2, all.Count());
        }

        public void Dispose()
        {
            Server.FlushDatabase();
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
