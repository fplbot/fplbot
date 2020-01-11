using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class TeamValue: ITeamValue
    {

        private readonly IEntryHistoryClient _historyClient;

        public TeamValue(IEntryHistoryClient historyClient)
        {
            _historyClient = historyClient;
        }

        public async Task<Dictionary<int, double>> GetTeamValuePerGameWeek(int teamCode)
        {
            var entryHistory = await _historyClient.GetHistory(teamCode);


            var teamValueMap = new Dictionary<int, double>();
            foreach (var entry in entryHistory.GameweekHistory)
            {

                teamValueMap.Add(entry.Event, entry.Value / 10.0);
            }

            return teamValueMap;
        }

        public async Task<Dictionary<int, double>> GetValueInBankPerGameWeek(int teamCode)
        {
            var entryHistory = await _historyClient.GetHistory(teamCode);

            var bankValueMap = new Dictionary<int, double>();
            foreach (var entry in entryHistory.GameweekHistory)
            {

                bankValueMap.Add(entry.Event, entry.Bank / 10.0);
            }

            return bankValueMap;
        }
    }
}
