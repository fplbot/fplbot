using Discord.Net.HttpClients;

namespace FplBot.Discord;

public record SlashCommandDefinition(string Name, string Description, ApplicationCommandOptions[] Options);

public record SlashCommandDefinitionSummary(string Name, string Description, string OptionsSummary);

public class DiscordSlashCommandsEnsurer(IDiscordClient client, ILogger<DiscordSlashCommandsEnsurer> logger)
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
        foreach (var command in GetDefinedCommands())
        {
            await client.ApplicationsCommandPost(command.Name, command.Description, guild, command.Options);
            await Task.Delay(3000);
        }
    }

    public async Task<IEnumerable<DiscordClient.ApplicationsCommand>> GetAllForGuild(string guildId)
    {
        return await client.ApplicationsCommandForGuildGet(guildId);
    }

    // The fixed set of commands `InstallGuildSlashCommandsInGuild` pushes to Discord — kept
    // as data so the admin UI can show admins what an install will actually register
    // (GetDefinedCommandSummaries) instead of just firing an opaque "queued" action.
    public static IReadOnlyList<SlashCommandDefinition> GetDefinedCommands() =>
    [
        new("help", "Shows help", []),
        new("follow", "Follow a FPL league in this channel", [
            new ApplicationCommandOptions
            {
                Type = 4, // leagueId as int
                Name = "leagueid",
                Description = "A FPL League Id.",
                Required = true
            }
        ]),
        new("subscriptions", "Manage subscription", [
            OptionWithOptions("add", OptionWithChoices("event")),
            OptionWithOptions("remove", OptionWithChoices("event"))
        ])
    ];

    public static IReadOnlyList<SlashCommandDefinitionSummary> GetDefinedCommandSummaries() =>
        GetDefinedCommands()
            .Select(c => new SlashCommandDefinitionSummary(c.Name, c.Description, Summarize(c.Options)))
            .ToList();

    private static string Summarize(ApplicationCommandOptions[] options)
    {
        return string.Join(", ", options.Select(SummarizeOption));
    }

    private static string SummarizeOption(ApplicationCommandOptions option)
    {
        if (option.Options is { Length: > 0 })
        {
            return $"{option.Name} {{{Summarize(option.Options)}}}";
        }

        return option.Required ? $"{option.Name}, required" : option.Name ?? "";
    }

    private static ApplicationCommandOptions OptionWithOptions(string name, params ApplicationCommandOptions[] subOpts)
    {
        return new ApplicationCommandOptions
        {
            Type = 1, // eventtype as suboption
            Name = name,
            Description = "add/remove",
            Options = subOpts
        };
    }

    private static ApplicationCommandOptions OptionWithChoices(string name)
    {
        return new ApplicationCommandOptions
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
    }
}
