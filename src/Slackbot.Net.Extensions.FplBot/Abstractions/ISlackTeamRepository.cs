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
        IAsyncEnumerable<SlackTeam> GetAllTeams();
        Task<IEnumerable<SlackTeam>> GetAllTeamsAsync();
        Task UpdateChannel(string teamId, string newChannel);
    }
    
    public class SlackTeam
    {
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string Scope { get; set; }
        public string AccessToken { get; set; }
        public string FplBotSlackChannel { get; set; }
        public long FplbotLeagueId { get; set; }
        public IEnumerable<EventSubscription> FplBotEventSubscriptions { get; set; } 
            = Enum.GetValues(typeof(EventSubscription)).Cast<EventSubscription>(); // TODO remove this initializer once we have stuff ready in db
    }

    public enum EventSubscription
    {
        Standings,
        Captains,
        Transfers,
        FixtureGoals,
        FixtureAssists,
        FixtureCards,
        FixturePenaltyMisses,
        Taunts,
        PriceChanges
    }
}