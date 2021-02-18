using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.WebApi.Pages.Admin.TeamDetails
{
    public class PublishEvent : PageModel
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly IMediator _mediator;
        private IGameweekClient _gameweekClient;

        public PublishEvent(ISlackTeamRepository teamRepo, ITokenStore tokenStore, IMediator mediator, IGameweekClient gameweekClient)
        {
            _teamRepo = teamRepo;
            _mediator = mediator;
            _gameweekClient = gameweekClient;
        }
        
        public async Task OnGet(string teamId)
        {
            var teamIdToUpper = teamId.ToUpper();
            Team = await _teamRepo.GetTeam(teamIdToUpper);
        }

        public async Task<IActionResult> OnPost(string teamId, EventSubscription[] subscriptions)
        {
            if (subscriptions == null || !subscriptions.Any())
            {
                TempData["msg"] += $"No subs selected..";
                return RedirectToPage(nameof(PublishEvent));
            }
            
            var teamIdToUpper = teamId.ToUpper();
            var team = await _teamRepo.GetTeam(teamIdToUpper);

            if (subscriptions.Contains(EventSubscription.Standings))
            {
                var gameweeks = await _gameweekClient.GetGameweeks();
                var gameweek = gameweeks.GetCurrentGameweek();
                await _mediator.Publish(new PublishStandingsCommand(team, gameweek));
                TempData["msg"] += $"Published standings to {teamId}";
            }
            else
            {
                TempData["msg"] += $"Unsupported event. Nothing published.";
            }
            
            return RedirectToPage(nameof(PublishEvent));
        }
        
        public SlackTeam Team { get; set; }
    }
}