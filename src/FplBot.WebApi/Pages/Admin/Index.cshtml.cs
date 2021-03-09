using System.Collections.Generic;
using System.Threading.Tasks;
using FplBot.Data.Abstractions;
using FplBot.Data.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
