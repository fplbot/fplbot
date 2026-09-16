using FplBot.Domain;

namespace FplBot.Data;

public interface IDomainRepository
{
    Task<Installation> GetInstallation(string teamId);
    Task Save(Installation installation);
    Task<Installation?> FindInstallationByTeamId(string teamId);
    Task<IEnumerable<Installation>> GetAllInstallations();
    Task Delete(Installation installation);
    Task<IEnumerable<(string InstallationId, string ChannelId)>> GetChannelsSubscribedTo(params FplEvent[] fplEvents);
    Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId);
    Task SaveChannelSubscription(string installationId, ChannelSubscription channel);
}
