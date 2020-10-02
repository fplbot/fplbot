using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        
        private readonly ISlackTeamRepository _teamRepo;
        

        public Index(ISlackClientBuilder builder, IOptions<SlackAppOptions> slackAppOptions, ISlackTeamRepository teamRepo, ITokenStore tokenStore, ILogger<Index> logger)
        {
            _teamRepo = teamRepo;
            Workspaces = new List<SlackTeam>();
        }
        
        public async Task OnGet()
        {
            await foreach (var t in _teamRepo.GetAllTeams())
            {
                Workspaces.Add(t);
            }
        }

        public List<SlackTeam> Workspaces { get; set; }
    }
}