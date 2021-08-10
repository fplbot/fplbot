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
        private readonly ILeagueIndexBookmarkProvider _leagueIndexBookmarkProvider;
        private readonly IEntryIndexBookmarkProvider _entryIndexBookmarkProvider;

        public Index(
            ISlackTeamRepository teamRepo,
            ILeagueIndexBookmarkProvider leagueIndexBookmarkProvider,
            IEntryIndexBookmarkProvider entryIndexBookmarkProvider)
        {
            _teamRepo = teamRepo;
            _leagueIndexBookmarkProvider = leagueIndexBookmarkProvider;
            _entryIndexBookmarkProvider = entryIndexBookmarkProvider;
            Workspaces = new List<SlackTeam>();
        }

        public async Task OnGet()
        {
            var teams = await _teamRepo.GetAllTeams();
            foreach (var t in teams)
            {
                Workspaces.Add(t);
            }

            CurrentLeagueIndexingBookmark = await _leagueIndexBookmarkProvider.GetBookmark();
            CurrentEntryIndexingBookmark = await _entryIndexBookmarkProvider.GetBookmark();
        }

        public List<SlackTeam> Workspaces { get; set; }
        public int CurrentLeagueIndexingBookmark { get; set; }
        public int CurrentEntryIndexingBookmark { get; set; }
    }
}
