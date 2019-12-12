using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client;
using Fpl.Client.Abstractions;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;

namespace FplBot.ConsoleApps.Handlers
{
    public class FplCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILeagueClient _leagueClient;

        public FplCommandHandler(IEnumerable<IPublisher> publishers, IGameweekClient gameweekClient, ILeagueClient leagueClient)
        {
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _leagueClient = leagueClient;
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("fpl", "Henter stillingen fra Blank-liga");
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var standings = await GetStandings();

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = standings
                });
            }

            return new HandleResponse(standings);
        }

        private async Task<string> GetStandings()
        {
            try
            {
                var league = await _leagueClient.GetClassicLeague(579157);
                var gameweeks = await _gameweekClient.GetGameweeks();
                var standings = Formatter.GetStandings(league, gameweeks);
                return standings;
            }
            catch (Exception e)
            {
                return $"Oops: {e.Message}";
            }
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("fpl");
        }

        public bool ShouldShowInHelp => true;
    }
}