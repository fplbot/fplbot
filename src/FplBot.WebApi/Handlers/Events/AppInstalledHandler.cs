using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Handlers.Events;

public class AppInstalledHandler(IGuildRepository guildRepo,
    ISlackTeamRepository slackRepo,
    ISlackClientBuilder builder,
    IConfiguration config,
    ILogger<AppInstalledHandler> logger) : IHandleMessages<AppInstalled>
{
    public async Task Handle(AppInstalled @event, IMessageHandlerContext context)
    {
        var count = @event.Platform switch
        {
            ChatPlatform.Discord => (await guildRepo.GetAllGuilds()).Count(),
            ChatPlatform.Slack => (await slackRepo.GetAllTeams()).Count(),
            _ => -1
        };

        var text = count switch
        {
            _ when count % 1000 == 0 => $"??🎉🎉🎉🏁✅ 🎂 {count} {@event.Platform} installs! ‼️ 👀",
            _ when count % 100 == 0 => $"💯{count} {@event.Platform} installs!",
            _ when count % 10 == 0 => $"{count} {@event.Platform} installs!",
            _ => null
        };

        if (text is not null)
        {
            logger.LogInformation("Sending count msg. {Count} {Platform} installs", count, @event.Platform);

            var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
            var env = config.GetValue<string>("DOTNET_ENVIRONMENT");
            var prefix = env == "Production" ? "" : $"{env}: ";
            var client = builder.Build(token);
            await client.ChatPostMessage("#fplbot-notifications", $"{prefix}{text}");
        }
        else
        {
            logger.LogInformation("No message sent for {Platform} install. Count is {Count}", @event.Platform, count);
        }
    }
}
