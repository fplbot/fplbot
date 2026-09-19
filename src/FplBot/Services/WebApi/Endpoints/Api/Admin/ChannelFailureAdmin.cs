using FplBot.Data;
using FplBot.EventHandlers;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record ChannelFailureStatsDto(int InstallationsWithFailures, int ChannelsWithFailures, int ChannelsEligibleForPurge);

public static class ChannelFailureAdmin
{
    public static async Task<ChannelFailureStatsDto> GetStats(IDomainRepository repo, DateTimeOffset now)
    {
        var failing = (await repo.GetAllInstallations())
            .SelectMany(i => i.ChannelSubscriptions.Select(c => (InstallationId: i.ExternalId, Channel: c)))
            .Where(x => x.Channel.FailureCount > 0)
            .ToList();

        return new ChannelFailureStatsDto(
            failing.Select(x => x.InstallationId).Distinct().Count(),
            failing.Count,
            failing.Count(x => x.Channel.IsStale(now)));
    }

    public static async Task<int> ResetAll(IDomainRepository repo, ILogger logger)
    {
        var cleared = 0;
        foreach (var installation in await repo.GetAllInstallations())
        {
            foreach (var channelId in installation.ChannelSubscriptions.Where(c => c.FailureCount > 0).Select(c => c.ChannelId))
            {
                if (await StaleChannelSubscriptions.ClearFailures(repo, installation.ExternalId, channelId, logger))
                {
                    cleared++;
                }
            }
        }

        return cleared;
    }
}
