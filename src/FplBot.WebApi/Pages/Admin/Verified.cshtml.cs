using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Slack.Extensions;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Models;
using FplBot.VerifiedEntries.InternalCommands;
using FplBot.WebApi.Handlers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace  FplBot.WebApi.Pages.Admin;

public class Verified : PageModel
{
    private readonly IVerifiedEntriesRepository _repo;
    private readonly IVerifiedPLEntriesRepository _plRepo;
    private readonly IGlobalSettingsClient _settings;
    private readonly IMediator _mediator;
    private readonly ILogger<Verified> _logger;

    public Verified(
        IVerifiedEntriesRepository repo,
        IVerifiedPLEntriesRepository plRepo,
        IEntryClient entryClient,
        IGlobalSettingsClient settings,
        IMediator mediator,
        ILogger<Verified> logger)
    {
        _repo = repo;
        _plRepo = plRepo;
        _settings = settings;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task OnGet()
    {
        _logger.LogInformation("Getting all");
        VerifiedEntries = (await _repo.GetAllVerifiedEntries()).OrderBy(x => x.FullName);
        VerifiedPLEntries = await _plRepo.GetAllVerifiedPLEntries();
        _logger.LogInformation("All fetched");
    }

    public async Task<IActionResult> OnPostSeedSelfishness()
    {
        await _mediator.Publish(new SeedSelfishness());
        TempData["msg"] += $"Selfishness added to seed!";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostDeleteAll()
    {
        _logger.LogInformation($"Deleting all");
        await _repo.DeleteAll();
        await _plRepo.DeleteAll();
        TempData["msg"] += $"All deleted";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostCleanUpDbMess()
    {
        _logger.LogInformation("Cleaning up db mess");

        var entries = await _repo.GetAllVerifiedEntries();
        var plEntries = await _plRepo.GetAllVerifiedPLEntries();
        var diff = plEntries.Select(x => x.EntryId).Except(entries.Select(x => x.EntryId)).ToArray();
        await _plRepo.DeleteAllOfThese(diff);

        TempData["msg"] += $"Deleted these pl entries: : {string.Join(", ", diff)}";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostUpdateEntry(UpdateEntry model, UpdateAction action)
    {
        if (action == UpdateAction.Del)
        {
            _logger.LogInformation("Removing single: {entryId}", model.EntryId);
            await _repo.Delete(model.EntryId);
            await _plRepo.Delete(model.EntryId);
            TempData["msg"] += $"Entry {model.EntryId} deleted!";
        }
        else
        {
            _logger.LogInformation("Updating single: {entryId}", model.EntryId);
            await _repo.Insert(new VerifiedEntry(
                model.EntryId,
                model.FullName,
                model.EntryTeamName,
                model.VerifiedEntryType,
                model.Alias,
                model.Description));
            TempData["msg"] += $"Entry {model.EntryId} updated!";
        }

        await _mediator.Publish(new IndexEntry(model.EntryId));

        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostAddEntry(AddEntry model)
    {
        if (!model.VerifiedEntryType.HasValue)
        {
            TempData["error"] += $"You must specify a {nameof(model.VerifiedEntryType)}";
            return RedirectToPage("Verified");
        }
        if (model.VerifiedEntryType == VerifiedEntryType.FootballerInPL && !model.PLPlayer.HasValue)
        {
            TempData["error"] += $"Cannot add {nameof(VerifiedEntryType.FootballerInPL)} without {nameof(model.PLPlayer)}";
            return RedirectToPage("Verified");
        }

        _logger.LogInformation("Adding new entry: {entryId}", model.EntryId);
        await _repo.Insert(new VerifiedEntry(
            model.EntryId,
            model.FullName,
            model.EntryTeamName,
            model.VerifiedEntryType.Value,
            model.Alias,
            model.Description));
        TempData["msg"] += $"Entry {model.EntryId} added!";

        await _mediator.Publish(new UpdateEntryStats(model.EntryId));

        if (model.PLPlayer.HasValue)
        {
            var settings = await _settings.GetGlobalSettings();
            var gameweek = settings.Gameweeks.GetCurrentGameweek();
            await _mediator.Publish(new ConnectEntryToPLPlayer(model.EntryId, model.PLPlayer.Value, gameweek?.Id));
        }

        await _mediator.Publish(new IndexEntry(model.EntryId));

        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostUpdateAllBaseStats()
    {
        var settings = await _settings.GetGlobalSettings();
        var gameweek = settings.Gameweeks.GetCurrentGameweek();
        await _mediator.Publish(new UpdateAllEntryStats());
        TempData["msg"] += $"Base stats added using gw {gameweek.Id}!";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostUpdateLiveScoreStats()
    {
        var settings = await _settings.GetGlobalSettings();
        var gameweek = settings.Gameweeks.GetCurrentGameweek();
        await _mediator.Publish(new UpdateVerifiedEntriesCurrentGwPointsCommand());
        TempData["msg"] += $"Base stats added using gw {gameweek.Id}!";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostIncrementSelfishStats()
    {
        var settings = await _settings.GetGlobalSettings();
        var gameweek = settings.Gameweeks.GetCurrentGameweek();
        await _mediator.Publish(new UpdateSelfishStats(Gameweek: gameweek.Id));
        TempData["msg"] += $"Selfish stats incremented using gw {gameweek.Id} numbers!";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostUpdateSelfPoints(int entryId, int points)
    {
        await _mediator.Publish(new IncrementPointsFromSelfOwnership(entryId, points));
        TempData["msg"] += $"Selfishness updated!";
        return RedirectToPage("Verified");
    }

    public async Task<IActionResult> OnPostIncrementWeekCounter(int entryId)
    {
        await _mediator.Publish(new IncrementSelfOwnershipWeekCounter(entryId));
        TempData["msg"] += $"Selfishness updated!";
        return RedirectToPage("Verified");
    }

    public IEnumerable<VerifiedEntry> VerifiedEntries { get; set; } = new List<VerifiedEntry>();

    public IEnumerable<VerifiedPLEntry> VerifiedPLEntries { get; set; } = new List<VerifiedPLEntry>();
}

public class UpdateEntry
{
    public int EntryId { get; set; }
    public string FullName { get; set; }
    public string EntryTeamName { get; set; }
    public VerifiedEntryType VerifiedEntryType { get; set; }
    public string Alias { get; set; }
    public string Description { get; set; }
}

public class AddEntry
{
    public int EntryId { get; set; }
    public string FullName { get; set; }
    public string EntryTeamName { get; set; }
    public VerifiedEntryType? VerifiedEntryType { get; set; }
    public string Alias { get; set; }
    public string Description { get; set; }
    public int? PLPlayer { get; set; }
}

public enum UpdateAction
{
    Save,
    Del
}