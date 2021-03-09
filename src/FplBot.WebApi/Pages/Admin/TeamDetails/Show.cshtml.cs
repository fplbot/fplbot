using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using FplBot.WebApi.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Pages.Admin.TeamDetails
{
    public class TeamDetailsIndex : PageModel
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ITokenStore _tokenStore;
        private readonly ISlackClientBuilder _builder;
        private readonly ILeagueClient _leagueClient;
        private readonly IOptions<OAuthOptions> _slackAppOptions;
        private readonly ILogger<TeamDetailsIndex> _logger;

        public TeamDetailsIndex(ISlackTeamRepository teamRepo, ITokenStore tokenStore, ILogger<TeamDetailsIndex> logger, IOptions<OAuthOptions> slackAppOptions, ISlackClientBuilder builder, ILeagueClient leagueClient)
        {
            _teamRepo = teamRepo;
            _tokenStore = tokenStore;
            _logger = logger;
            _slackAppOptions = slackAppOptions;
            _builder = builder;
            _leagueClient = leagueClient;
        }

        public async Task OnGet(string teamId)
        {
            var teamIdToUpper = teamId.ToUpper();
            var team = await _teamRepo.GetTeam(teamIdToUpper);
            if (team != null)
            {
                Team = team;
                var league = await _leagueClient.GetClassicLeague((int) team.FplbotLeagueId);
                League = league;
                var slackClient = await CreateSlackClient(teamIdToUpper);
                try
                {
                    var channels = await slackClient.ConversationsListPublicChannels(500);
                    ChannelStatus = channels.Channels.FirstOrDefault(c => team.FplBotSlackChannel == $"#{c.Name}") != null;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public ClassicLeague League { get; set; }
        public bool? ChannelStatus { get; set; }

        public async Task<IActionResult> OnPost(string teamId)
        {
            _logger.LogInformation($"Deleting {teamId}");
            var slackClient = await CreateSlackClient(teamId);
            var res = await slackClient.AppsUninstall(_slackAppOptions.Value.CLIENT_ID, _slackAppOptions.Value.CLIENT_SECRET);
            if (res.Ok)
            {
                TempData["msg"] = "Uninstall queued, and will be handled at some point";
            }
            else
            {
                TempData["msg"] = $"Uninstall failed '{res.Error}'";
            }

            return RedirectToPage("Index");
        }

        private async Task<ISlackClient> CreateSlackClient(string teamId)
        {
            var token = await _tokenStore.GetTokenByTeamId(teamId);
            var slackClient = _builder.Build(token: token);
            return slackClient;
        }
        public SlackTeam Team { get; set; }
    }
}
