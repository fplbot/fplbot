using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net.HttpClients;
using FplBot.Discord.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FplBot.Discord
{
    public class DiscordSlashCommandsEnsurer : IHostedService
    {
        private readonly DiscordClient _client;
        private readonly ILogger<DiscordSlashCommandsEnsurer> _logger;

        public DiscordSlashCommandsEnsurer(DiscordClient client, ILogger<DiscordSlashCommandsEnsurer> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InstallSlashCommandsInGuild();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        // TODO: Migrate to global slash commands when ready
        // These will only be available to the single guild
        private async Task InstallSlashCommandsInGuild()
        {
            string fplBotGuildId = "893932860162064414"; // test guild

            await _client.ApplicationsCommandForGuildPost(
                "subs",
                "Shows active subscriptions",
                fplBotGuildId);

            // await _client.ApplicationsCommandForGuildPost(
            //     "help",
            //     "Shows help",
            //     fplBotGuildId);
            //
            // var applicationCommandOptions = new ApplicationCommandOptions
            // {
            //     Type = 4, // leagueId as int
            //     Name = "leagueid",
            //     Description = "A FPL League Id.",
            //     Required = true
            // };
            //
            // await _client.ApplicationsCommandForGuildPost("follow",
            //     "Follow a FPL league in this channel",
            //     fplBotGuildId,
            //     applicationCommandOptions);

            await _client.ApplicationsCommandForGuildPost("subscriptions",
                "Manage subscription",
                fplBotGuildId,
                SubOption("add", "event"), SubOption("remove","event"));

            // await _client.ApplicationsCommandForGuildPost("unsubscribe",
            //     "Unsubscribe to event",
            //     fplBotGuildId,
            //     EventOption("events"));

            ApplicationCommandOptions SubOption(string name, params string[] subOpts)
            {
                var subCommand = new ApplicationCommandOptions()
                {
                    Type = 1, // eventtype as suboption
                    Name = name,
                    Description = "add/remove",
                    Options = subOpts.Select(SubgroupOption).ToArray()
                };
                return subCommand;
            }

            ApplicationCommandOptions SubgroupOption(string name)
            {
                var subscribeOption = new ApplicationCommandOptions()
                {
                    Type = 3, // eventtype as string
                    Name = name,
                    Description = "Available events",
                    Required = true,
                    Choices = Enum.GetNames<EventSubscription>().Select(e => new ApplicationCommandChoices
                    {
                        Name = e,
                        Value = e
                    }).ToArray()
                };
                return subscribeOption;
            }
        }
    }
}
