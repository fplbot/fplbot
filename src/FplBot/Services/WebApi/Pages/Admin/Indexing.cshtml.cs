using Fpl.Search.Data.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin;

public class Indexing(
    ILeagueIndexBookmarkProvider leagueIndexBookmarkProvider,
    IEntryIndexBookmarkProvider entryIndexBookmarkProvider)
    : PageModel
{
    public async Task OnGet()
    {
        CurrentLeagueIndexingBookmark = await leagueIndexBookmarkProvider.GetBookmark();
        CurrentEntryIndexingBookmark = await entryIndexBookmarkProvider.GetBookmark();
    }

    public async Task<IActionResult> OnPostChangeLeagueIndexingBookmark(ChangeBookmarkModel model)
    {
        await leagueIndexBookmarkProvider.SetBookmark(model.Bookmark);
        TempData["msg"] += "League bookmark updated";
        return RedirectToPage("Indexing");
    }

    public async Task<IActionResult> OnPostChangeEntryIndexingBookmark(ChangeBookmarkModel model)
    {
        await entryIndexBookmarkProvider.SetBookmark(model.Bookmark);
        TempData["msg"] += "Entry bookmark updated";
        return RedirectToPage("Indexing");
    }

    public int CurrentLeagueIndexingBookmark { get; set; }
    public int CurrentEntryIndexingBookmark { get; set; }
}

public class ChangeBookmarkModel
{
    public int Bookmark { get; set; }
}
