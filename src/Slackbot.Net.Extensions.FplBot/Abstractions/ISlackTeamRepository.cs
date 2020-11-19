using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Abstractions
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
        Taunts,
        PriceChanges,
        InjuryUpdates,
        // TODO: Deadline
    }
}