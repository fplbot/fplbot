using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplStandingsCommandHandler : IHandleMessages
    {
        private readonly IOptions<FplbotOptions> _options;
        private readonly IEnumerable<IPublisherBuilder> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILeagueClient _leagueClient;

        public FplStandingsCommandHandler(IOptions<FplbotOptions> options, IEnumerable<IPublisherBuilder> publishers, IGameweekClient gameweekClient, ILeagueClient leagueClient)
        {
            _options = options;
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _leagueClient = leagueClient;
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var standings = await GetStandings();

            foreach (var pBuilder in _publishers)
            {
                var p = await pBuilder.Build(message.Team.Id);
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
                var leagueTask = _leagueClient.GetClassicLeague(_options.Value.LeagueId);
                var gameweeksTask = _gameweekClient.GetGameweeks();
                var standings = Formatter.GetStandings(await leagueTask, await gameweeksTask);
                return standings;
            }
            catch (Exception e)
            {
                return $"Oops: {e.Message}";
            }
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("standings");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("standings", "Get current league standings");
        public bool ShouldShowInHelp => true;
    }
}