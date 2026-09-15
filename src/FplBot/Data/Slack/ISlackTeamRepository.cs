using FplBot.Data;
using FplBot.Domain;

namespace FplBot.Data.Slack;

public interface ISlackTeamRepository : IDomainRepository
{
    Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string teamId);
}
