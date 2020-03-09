using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        private readonly ITokenStore _tokenStore;
        private readonly IFetchFplbotSetup _setups;
        private readonly ISlackTeamRepository _teamRepo;

        public Index(ITokenStore tokenStore, IFetchFplbotSetup setups, ISlackTeamRepository teamRepo)
        {
            _tokenStore = tokenStore;
            _setups = setups;
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