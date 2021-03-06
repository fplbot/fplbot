using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fpl.Data.Repositories
{
    public interface ISlackTeamRepository
    {
        Task<SlackTeam> GetTeam(string teamId);
        Task UpdateLeagueId(string teamId, long newLeagueId);
        Task DeleteByTeamId(string teamId);
        Task Insert(SlackTeam slackTeam);
        Task<IEnumerable<SlackTeam>> GetAllTeams();
        Task UpdateChannel(string teamId, string newChannel);
        Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions);
    }

    public class SlackTeam
    {
        public SlackTeam()
        {
            Subscriptions = new List<EventSubscription>();
        }

        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string Scope { get; set; }
        public string AccessToken { get; set; }
        public string FplBotSlackChannel { get; set; }
        public long FplbotLeagueId { get; set; }

        /// <summary>
        /// WIP
        /// </summary>
        public IEnumerable<EventSubscription> Subscriptions { get; set; }
    }

    public enum EventSubscription
    {
        All,
        Standings,
        Captains,
        Transfers,
        FixtureGoals,
        FixtureAssists,
        FixtureCards,
        FixturePenaltyMisses,
        FixtureFullTime,
        Taunts,
        PriceChanges,
        InjuryUpdates,
        Deadlines,
        Lineups
    }
}
