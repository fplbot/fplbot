using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Data.Abstractions;
using FplBot.Core.Data.Models;
using FplBot.Core.Helpers.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Core.Handlers.FplEvents
{
    public class FixtureFulltimeHandler : IHandleMessages<FixtureFinished>, IHandleMessages<PublishFulltimeMessageToSlackWorkspace>
    {
        private readonly ISlackClientBuilder _builder;
        private readonly ISlackTeamRepository _slackTeamRepo;
        private readonly ILogger<FixtureFulltimeHandler> _logger;
        private readonly IGlobalSettingsClient _settingsClient;
        private readonly IFixtureClient _fixtureClient;

        public FixtureFulltimeHandler(ISlackClientBuilder builder, ISlackTeamRepository slackTeamRepo, ILogger<FixtureFulltimeHandler> logger, IGlobalSettingsClient settingsClient, IFixtureClient fixtureClient)
        {
            _builder = builder;
            _slackTeamRepo = slackTeamRepo;
            _logger = logger;
            _settingsClient = settingsClient;
            _fixtureClient = fixtureClient;
        }

        public async Task Handle(FixtureFinished message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Handling fixture full time");
            var teams = await _slackTeamRepo.GetAllTeams();
            var settings = await _settingsClient.GetGlobalSettings();
            var fixtures = await _fixtureClient.GetFixtures();
            var fplfixture = fixtures.FirstOrDefault(f => f.Id == message.FixtureId);
            var fixture = CreateFinishedFixture(settings.Teams, settings.Players, fplfixture);
            var title = $"*FT: {fixture.HomeTeam.ShortName} {fixture.Fixture.HomeTeamScore}-{fixture.Fixture.AwayTeamScore} {fixture.AwayTeam.ShortName}*";
            var threadMessage = Formatter.FormatProvisionalFinished(fixture);

            foreach (var slackTeam in teams)
            {
                if (slackTeam.HasRegisteredFor(EventSubscription.FixtureFullTime))
                {
                    await context.SendLocal(new PublishFulltimeMessageToSlackWorkspace(slackTeam.TeamId, title, threadMessage));
                }
            }
        }

        private static FinishedFixture CreateFinishedFixture(ICollection<Team> teams, ICollection<Player> players, Fixture n)
        {
            return new FinishedFixture
            {
                Fixture = n,
                HomeTeam = teams.First(t => t.Id == n.HomeTeamId),
                AwayTeam = teams.First(t => t.Id == n.AwayTeamId),
                BonusPoints = CreateBonusPlayers(players, n)
            };
        }

        private static IEnumerable<BonusPointsPlayer> CreateBonusPlayers(ICollection<Player> players, Fixture fixture)
        {
            try
            {
                var bonusPointsHome = fixture.Stats.FirstOrDefault(s => s.Identifier == "bps")?.HomeStats;
                var bonusPointsAway = fixture.Stats.FirstOrDefault(s => s.Identifier == "bps")?.AwayStats;

                var home = bonusPointsHome.Select(BpsFilter).ToList();
                var away = bonusPointsAway.Select(BpsFilter).ToList();
                var aggregated = home.Concat(away).OrderByDescending(bpp => bpp.BonusPoints);
                return aggregated;

                BonusPointsPlayer BpsFilter(FixtureStatValue bps)
                {
                    return new BonusPointsPlayer
                    {
                        Player = players.First(p => p.Id == bps.Element),
                        BonusPoints = bps.Value
                    };
                }
            }
            catch
            {
                return new List<BonusPointsPlayer>();
            }
        }

        public async Task Handle(PublishFulltimeMessageToSlackWorkspace message, IMessageHandlerContext context)
        {
            var team = await _slackTeamRepo.GetTeam(message.WorkspaceId);
            var slackClient = _builder.Build(team.AccessToken);
            var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, message.Title);
            if(!string.IsNullOrEmpty(message.ThreadMessage) && res.Ok)
            {
                await slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = team.FplBotSlackChannel, thread_ts = res.ts, Text = message.ThreadMessage, unfurl_links = "false"
                });
            }
        }
    }

    public class FinishedFixture
    {
        public Fixture Fixture { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }

        public IEnumerable<BonusPointsPlayer> BonusPoints { get; set; } = new List<BonusPointsPlayer>();
    }

    public class BonusPointsPlayer
    {
        public Player Player { get; set; }
        public int BonusPoints { get; set; }
    }
}
