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
    IHostEnvironment env,
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

        var definition = context.Message.Platform == ChatPlatform.Discord ? "Discord guild" : "Slack workspace";
        var installMsg = $"🎉 A new {definition} ('{context.Message.TeamName}') installed @fplbot!";
        var fullMsg = text is not null ? $"{installMsg} {text}" : installMsg;

        if (text is not null)
            logger.LogInformation("Sending count msg. {Count} {Platform} installs", count, context.Message.Platform);
        else
            logger.LogInformation("No count msg for {Platform} install. Count is {Count}", context.Message.Platform, count);

        var envName = config.GetValue<string>("DOTNET_ENVIRONMENT");
        var prefix = envName == "Production" ? "" : $"{envName}: ";
        var message = $"{prefix}{fullMsg}";

        if (env.IsDevelopment())
        {
            logger.LogInformation("[DEV] Slack → #fplbot-notifications\n{Message}", message);
            return;
        }

        var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
        var client = builder.Build(token);
        await client.ChatPostMessage("#fplbot-notifications", message);
    }
}
