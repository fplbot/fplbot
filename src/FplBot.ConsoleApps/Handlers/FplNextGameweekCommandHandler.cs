using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Publishers;

namespace FplBot.ConsoleApps.Handlers
{
    public class FplNextGameweekCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IGameweekClient _gameweekClient;
        private readonly IFixtureClient _fixtureClient;
        private readonly ITeamsClient _teamsclient;

        public FplNextGameweekCommandHandler(IEnumerable<IPublisher> publishers, IGameweekClient gameweekClient, IFixtureClient fixtureClient, ITeamsClient teamsclient)
        {
            _publishers = publishers;
            _gameweekClient = gameweekClient;
            _fixtureClient = fixtureClient;
            _teamsclient = teamsclient;
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("next", "Henter neste gameweek");
        }

        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var gameweeks = await _gameweekClient.GetGameweeks();
            var nextGw = gameweeks.First(gw => gw.IsNext);
            var fixtures = await _fixtureClient.GetFixturesByGameweek(nextGw.Id);
            var teams = await _teamsclient.GetAllTeams();

            var textToSend = TextToSend(nextGw, fixtures, teams);

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = textToSend
                });
            }

            return new HandleResponse(textToSend);
        }

        private static string TextToSend(Gameweek gameweek, ICollection<Fixture> fixtures, ICollection<Team> teams)
        {
            var textToSend = $":information_source: <https://fantasy.premierleague.com/fixtures/{gameweek.Id}|{gameweek.Name.ToUpper()}>";
            textToSend += $"\nDeadline: {ConvertToNorwegianTimeZone(gameweek.Deadline).ToString("yyyy-MM-dd HH:mm")}\n";
            
            var groupedByDay = fixtures.GroupBy(f => f.KickOffTime.Value.Date);

            foreach (var group in groupedByDay)
            {
                textToSend += $"\n{ConvertToNorwegianTimeZone(group.Key).ToString("dddd")}";
                foreach (var fixture in group)
                {
                    var homeTeam = teams.First(t => t.Id == fixture.HomeTeamId);
                    var awayTeam = teams.First(t => t.Id == fixture.AwayTeamId);
                    var fixtureKickOffTime = ConvertToNorwegianTimeZone(fixture.KickOffTime.Value);
                    textToSend += $"\n•{fixtureKickOffTime.ToString("HH:mm")} {homeTeam.ShortName}-{awayTeam.ShortName}";
                }

                textToSend += "\n";
            }

            return textToSend;
        }
        
        private static DateTime ConvertToNorwegianTimeZone(DateTime dateToConvertTime)
        {
            var timeZoneId = @"Europe/Oslo";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                timeZoneId = "Central European Standard Time";
            }

            var norwegianZoneId = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTime(dateToConvertTime, norwegianZoneId);
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("next");
        }

        public bool ShouldShowInHelp => true;
    }
}