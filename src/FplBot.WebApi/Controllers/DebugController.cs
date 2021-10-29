using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DebugController
    {
        private readonly IMessageSession _session;
        private readonly IWebHostEnvironment _env;

        public DebugController(IMessageSession session, IWebHostEnvironment env)
        {
            _session = session;
            _env = env;
        }

        [HttpGet("goal")]
        public async Task<IActionResult> Goal(bool removed = false)
        {
            if (!_env.IsProduction())
            {
                // await _session.Publish(FixtureEvents(StatType.GoalsScored, removed));
                // await _session.Publish(FixtureEvents(StatType.Assists, removed));
                // await _session.Publish(FixtureEvents(StatType.OwnGoals, removed));
                // await _session.Publish(FixtureEvents(StatType.PenaltiesMissed, removed));
                // await _session.Publish(FixtureEvents(StatType.PenaltiesSaved, removed));
                // await _session.Publish(FixtureEvents(StatType.RedCards, removed));
                await _session.Publish(FixtureEvents(StatType.YellowCards, removed));
                await _session.Publish(FixtureEvents(StatType.Saves, removed));
                await _session.Publish(FixtureEvents(StatType.Bonus, removed));
                return new OkResult();
            }

            return new UnauthorizedResult();

        }

        private static FixtureEventsOccured FixtureEvents(StatType type, bool isRemoved)
        {
            List<FixtureEvents> fixtureEventsList = new();
            FixtureTeam home = new(1, "HOM", "HomeTeam");
            FixtureTeam away = new(2, "AWA", "Away");
            FixtureScore fixtureScore = new(home, away, 0, 0);
            Dictionary<StatType,List<PlayerEvent>> statMap = new();
            PlayerDetails playerDetails1 = new(1,"First", "Last", "Testerson");
            PlayerDetails playerDetails2 = new(2,"Yolo", "Yolsen", "Yolerson");
            TeamType teamDetails = TeamType.Home;
            List<PlayerEvent> playerEvents = new()
            {
                new PlayerEvent(playerDetails1, teamDetails, IsRemoved: isRemoved),
                new PlayerEvent(playerDetails2, teamDetails, false)
            };
            statMap.Add(type, playerEvents);
            fixtureEventsList.Add(new FixtureEvents(fixtureScore, statMap));
            return new FixtureEventsOccured(fixtureEventsList);
        }
    }
}
