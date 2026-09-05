using System.Net;
using CronBackgroundServices;
using Discord.Net.HttpClients;
using FplBot.Data.Discord;

namespace FplBot.WebApi.Infrastructure;

public class GuildStatusChecker(IGuildRepository guildRepo, DiscordClient discordClient, ILogger<GuildStatusChecker> logger) : IRecurringAction
{
    public async Task Process(CancellationToken stoppingToken)
    {
        var allGuilds = await guildRepo.GetAllGuilds();
        var allGuildSubs = await guildRepo.GetAllGuildSubscriptions();
        var counter = 0;
        foreach (var guild in allGuilds)
        {
            var guildSubs = allGuildSubs.Where(s => s.GuildId == guild.Id).ToList();

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
                foreach (var sub in guildSubs)
                {
                    await guildRepo.DeleteGuildSubscription(sub.GuildId, sub.ChannelId);
                    logger.LogInformation("AccessCheck: {GuildId} Deleted sub {ChannelId}.",
                        guild.Id, sub.ChannelId);
                }
                await guildRepo.DeleteGuild(guild.Id);
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
