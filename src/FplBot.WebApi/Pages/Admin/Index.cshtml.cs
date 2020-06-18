using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _setups;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<Index> _logger;

        public Index(ITokenStore tokenStore, IFetchFplbotSetup setups, ISlackTeamRepository teamRepo, ILogger<Index> logger)
        {
            _tokenStore = tokenStore;
            _setups = setups;
            _teamRepo = teamRepo;
            _logger = logger;
            Workspaces = new List<SlackTeam>();
        }
        
        public async Task OnGet()
        {
            await foreach (var t in _teamRepo.GetAllTeams())
            {
                Workspaces.Add(t);
            }
        }
        
        public async Task<IActionResult> OnPost(string teamId)
        {
            _logger.LogInformation($"Deleting {teamId}");
            
            // Should switch to https://api.slack.com/methods/apps.uninstall & let the event handling take care of delete in DB
            await _teamRepo.DeleteByTeamId(teamId);
            
            return RedirectToPage("Index");
        }

        public List<SlackTeam> Workspaces { get; set; }
    }
}