using Microsoft.AspNetCore.Mvc.RazorPages;
using Fpl.Search.Data.Abstractions;
using FplBot.Data.Slack;

namespace FplBot.WebApi.Pages.Admin;

public class Index(
    ISlackTeamRepository teamRepo,
    ILeagueIndexBookmarkProvider leagueIndexBookmarkProvider,
    IEntryIndexBookmarkProvider entryIndexBookmarkProvider)
    : PageModel
{
    public async Task OnGet()
    {
        var teams = await teamRepo.GetAllTeams();
        foreach (var t in teams)
        {
            Workspaces.Add(t);
        }

        CurrentLeagueIndexingBookmark = await leagueIndexBookmarkProvider.GetBookmark();
        CurrentEntryIndexingBookmark = await entryIndexBookmarkProvider.GetBookmark();
    }

    public List<SlackTeam> Workspaces { get; set; } = new();
    public int CurrentLeagueIndexingBookmark { get; set; }
    public int CurrentEntryIndexingBookmark { get; set; }
}
