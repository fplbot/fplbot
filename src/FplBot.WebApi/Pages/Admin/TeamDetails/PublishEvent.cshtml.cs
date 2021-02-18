using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.WebApi.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Pages.Admin.TeamDetails
{
    public class PublishEvent : PageModel
    {
        private readonly ISlackTeamRepository _teamRepo;
        

        public PublishEvent(ISlackTeamRepository teamRepo, ITokenStore tokenStore, ILogger<TeamDetailsIndex> logger, IOptions<OAuthOptions> slackAppOptions, ISlackClientBuilder builder, ILeagueClient leagueClient)
        {
            _teamRepo = teamRepo;
        }
        
        public async Task OnGet(string teamId)
        {
            var teamIdToUpper = teamId.ToUpper();
            Team = await _teamRepo.GetTeam(teamIdToUpper);
        }

        public async Task<IActionResult> OnPost(string teamId, EventSubscription[] subscriptions)
        {
            string subs = subscriptions != null && subscriptions.Any() ? subscriptions?.Select(x => x.ToString()).Aggregate((x,y) => x + "," + y)  : "No selected";
            TempData["msg"] += $"Yup! {teamId} {subs}";
            return RedirectToPage(nameof(PublishEvent));
        }
        
        public SlackTeam Team { get; set; }
    }
}