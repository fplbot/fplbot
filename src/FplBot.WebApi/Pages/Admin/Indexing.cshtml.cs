using Fpl.Search.Data.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin;

public class Indexing : PageModel
{
    private readonly ILeagueIndexBookmarkProvider _leagueIndexBookmarkProvider;
    private readonly IEntryIndexBookmarkProvider _entryIndexBookmarkProvider;

    public Indexing(
        ILeagueIndexBookmarkProvider leagueIndexBookmarkProvider,
        IEntryIndexBookmarkProvider entryIndexBookmarkProvider)
    {
        _leagueIndexBookmarkProvider = leagueIndexBookmarkProvider;
        _entryIndexBookmarkProvider = entryIndexBookmarkProvider;
    }

    public async Task OnGet()
    {
        CurrentLeagueIndexingBookmark = await _leagueIndexBookmarkProvider.GetBookmark();
        CurrentEntryIndexingBookmark = await _entryIndexBookmarkProvider.GetBookmark();
    }

    public async Task<IActionResult> OnPostChangeLeagueIndexingBookmark(ChangeBookmarkModel model)
    {
        await _leagueIndexBookmarkProvider.SetBookmark(model.Bookmark);
        TempData["msg"] += "League bookmark updated";
        return RedirectToPage("Indexing");
    }

    public async Task<IActionResult> OnPostChangeEntryIndexingBookmark(ChangeBookmarkModel model)
    {
        await _entryIndexBookmarkProvider.SetBookmark(model.Bookmark);
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