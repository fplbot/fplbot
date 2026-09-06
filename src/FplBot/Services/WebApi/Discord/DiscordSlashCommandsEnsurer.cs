using Discord.Net.HttpClients;

namespace FplBot.Discord;

public class DiscordSlashCommandsEnsurer(DiscordClient client, ILogger<DiscordSlashCommandsEnsurer> logger)
{
    private readonly ILogger<DiscordSlashCommandsEnsurer> _logger = logger;

    public async Task DeleteGuildSlashCommands(string guild)
    {
        var applicationsCommands = await client.ApplicationsCommandForGuildGet(guild);

        foreach (var applicationsCommand in applicationsCommands)
        {
            await client.ApplicationsCommandForGuildDelete(guild, applicationsCommand.Id);
            await Task.Delay(5000);
        }
    }

    public async Task InstallGuildSlashCommandsInGuild(string? guild = null)
    {
        await client.ApplicationsCommandPost(
            "help",
            "Shows help",
            guild);

        await Task.Delay(3000);

        await Task.Delay(3000);

        await client.ApplicationsCommandPost("follow",
            "Follow a FPL league in this channel",
            guild,
            new ApplicationCommandOptions
            {
                Type = 4, // leagueId as int
                Name = "leagueid",
                Description = "A FPL League Id.",
                Required = true
            });

        await Task.Delay(3000);

        await client.ApplicationsCommandPost("subscriptions",
            "Manage subscription",
            guild,
            OptionWithOptions("add", OptionWithChoices("event")),
            OptionWithOptions("remove", OptionWithChoices("event")));

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

    public async Task<IEnumerable<DiscordClient.ApplicationsCommand>> GetAllForGuild(string guildId)
    {
        return await client.ApplicationsCommandForGuildGet(guildId);
    }
}
