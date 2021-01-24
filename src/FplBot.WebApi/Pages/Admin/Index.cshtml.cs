using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Search.Indexing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nest;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace FplBot.WebApi.Pages.Admin
{
    public class Index : PageModel
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly IIndexBookmarkProvider _indexBookmarkProvider;


        public Index(
            ISlackTeamRepository teamRepo,
            IIndexBookmarkProvider indexBookmarkProvider)
        {
            _teamRepo = teamRepo;
            _indexBookmarkProvider = indexBookmarkProvider;
            Workspaces = new List<SlackTeam>();
        }
        
        public async Task OnGet()
        {
            var teams = await _teamRepo.GetAllTeams();
            foreach (var t in teams)
            {
                Workspaces.Add(t);
            }

            CurrentLeagueIndexingBookmark = await _indexBookmarkProvider.GetBookmark();
        }

        public List<SlackTeam> Workspaces { get; set; }
        public int CurrentLeagueIndexingBookmark { get; set; }
    }
}