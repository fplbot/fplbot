using FplBot.Domain;

namespace FplBot.Data.Slack;

public interface ISlackTeamRepository
{
    Task<Installation> GetInstallation(string teamId);
    Task Save(Installation installation);
    Task<Installation?> FindInstallationByTeamId(string teamId);
    Task<IEnumerable<Installation>> GetAllInstallations();

    Task Delete(Installation installation);

    Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string teamId);

    Task DeleteChannelSubscription(string teamId, string channelId);
}
