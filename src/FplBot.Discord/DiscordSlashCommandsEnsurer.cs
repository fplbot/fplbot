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
            //Task.Run(InstallSlashCommandsInGuild);
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
            var applicationsCommands = await _client.ApplicationsCommandForGuildGet(fplBotGuildId);
            foreach (var applicationsCommand in applicationsCommands)
            {
                //await _client.ApplicationsCommandForGuildDelete(fplBotGuildId, applicationsCommand.Id);
                await Task.Delay(5000);
            }

            await _client.ApplicationsCommandForGuildPost(
                "subs",
                "Shows active subscriptions",
                fplBotGuildId);

            await Task.Delay(3000);

            await _client.ApplicationsCommandForGuildPost(
                "help",
                "Shows help",
                fplBotGuildId);
            await Task.Delay(3000);

            var applicationCommandOptions = new ApplicationCommandOptions
            {
                Type = 4, // leagueId as int
                Name = "leagueid",
                Description = "A FPL League Id.",
                Required = true
            };
            await Task.Delay(3000);

            await _client.ApplicationsCommandForGuildPost("follow",
                "Follow a FPL league in this channel",
                fplBotGuildId,
                applicationCommandOptions);
            await Task.Delay(3000);

            await _client.ApplicationsCommandForGuildPost("subscriptions",
                "Manage subscription",
                fplBotGuildId,
                OptionWithOptions("add", OptionWithChoices("event")), OptionWithOptions("remove",OptionWithChoices("event")));


            ApplicationCommandOptions OptionWithOptions(string name, params ApplicationCommandOptions[] subOpts)
            {
                var subCommand = new ApplicationCommandOptions()
                {
                    Type = 1, // eventtype as suboption
                    Name = name,
                    Description = "add/remove",
                    Options = subOpts
                };
                return subCommand;
            }

            ApplicationCommandOptions OptionWithChoices(string name)
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
