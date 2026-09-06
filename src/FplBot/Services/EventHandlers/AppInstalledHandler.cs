using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers;

public class AppInstalledHandler(IGuildRepository guildRepo,
    ISlackTeamRepository slackRepo,
    ISlackClientBuilder builder,
    IConfiguration config,
    ILogger<AppInstalledHandler> logger) : IConsumer<AppInstalled>
{
    public async Task Consume(ConsumeContext<AppInstalled> context)
    {
        var count = context.Message.Platform switch
        {
            ChatPlatform.Discord => (await guildRepo.GetAllGuilds()).Count(),
            ChatPlatform.Slack => (await slackRepo.GetAllTeams()).Count(),
            _ => -1
        };

        var text = count switch
        {
            _ when count % 1000 == 0 => $"??🎉🎉🎉🏁✅ 🎂 {count} {context.Message.Platform} installs! ‼️ 👀",
            _ when count % 100 == 0 => $"💯{count} {context.Message.Platform} installs!",
            _ when count % 10 == 0 => $"{count} {context.Message.Platform} installs!",
            _ => null
        };

        if (text is not null)
        {
            logger.LogInformation("Sending count msg. {Count} {Platform} installs", count, context.Message.Platform);

            var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
            var env = config.GetValue<string>("DOTNET_ENVIRONMENT");
            var prefix = env == "Production" ? "" : $"{env}: ";
            var client = builder.Build(token);
            await client.ChatPostMessage("#fplbot-notifications", $"{prefix}{text}");
        }
        else
        {
            logger.LogInformation("No message sent for {Platform} install. Count is {Count}", context.Message.Platform, count);
        }
    }
}
