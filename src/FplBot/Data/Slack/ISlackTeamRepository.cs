using FplBot.Domain;

namespace FplBot.Data.Slack;

public interface ISlackTeamRepository
{
    Task<SlackInstallation> GetInstallation(string teamId);
    Task Save(SlackInstallation installation);
    Task<SlackInstallation?> FindInstallationByTeamId(string teamId);
    Task<IEnumerable<SlackInstallation>> GetAllInstallations();

    Task Delete(SlackInstallation installation);

    Task<IEnumerable<SlackChannelSubscription>> GetChannelSubscriptions(string teamId);

    Task DeleteChannelSubscription(string teamId, string channelId);
}
