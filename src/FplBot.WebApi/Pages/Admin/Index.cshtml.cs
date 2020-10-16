using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        
        private readonly ISlackTeamRepository _teamRepo;
        

        public Index(ISlackTeamRepository teamRepo)
        {
            _teamRepo = teamRepo;
            Workspaces = new List<SlackTeam>();
        }
        
        public async Task OnGet()
        {
            var teams = await _teamRepo.GetAllTeams();
            foreach (var t in teams)
            {
                Workspaces.Add(t);
            }
        }

        public List<SlackTeam> Workspaces { get; set; }
    }
}