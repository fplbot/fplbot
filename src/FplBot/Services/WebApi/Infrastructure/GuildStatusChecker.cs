using System.Net;
using CronBackgroundServices;
using Discord.Net.HttpClients;
using FplBot.Data.Discord;

namespace FplBot.WebApi.Infrastructure;

public class GuildStatusChecker(IGuildRepository guildRepo, DiscordClient discordClient, ILogger<GuildStatusChecker> logger) : IRecurringAction
{
    public async Task Process(CancellationToken stoppingToken)
    {
        using var activity = FplBotDiagnostics.For(FplBotService.WebApi).StartActivity(nameof(GuildStatusChecker));
        var installations = await guildRepo.GetAllInstallations();
        var counter = 0;
        foreach (var guild in installations)
        {
            try
            {
                var fetchedGuild = await discordClient.GuildGet(guild.Id);
                logger.LogDebug("AccessCheck: Access to guild {GuildId} OK.", fetchedGuild.Id);
            }
            catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.NotFound)
            {
                counter++;
                logger.LogInformation("AccessCheck: {GuildId} ('{GuildName}') Guild #{Count} unknown to fplbot. "
                    , counter, guild.Id, guild.Name);
                await guildRepo.Delete(guild);
                logger.LogInformation("AccessCheck: {GuildId} ('{GuildName}') Guild deleted ❌", guild.Id, guild.Name);
            }
            catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.Forbidden)
            {
                logger.LogWarning("AccessCheck: {GuildId} ('{GuildName}') Got forbidden. {Message} ", guild.Id, guild.Name, hre.Message);
                await Task.Delay(500, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogInformation("AccessCheck: {GuildId} ('{GuildName}') SKIPPED!. Exception: '{ExceptionMessage}'", guild.Id, guild.Name, e);
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    public string Cron => "0 1 */1 * * *";
}
