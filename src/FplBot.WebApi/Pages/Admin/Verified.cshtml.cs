using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Search.Models;
using FplBot.Core.Data;
using FplBot.Core.Extensions;
using FplBot.Core.Handlers.InternalCommands;
using FplBot.WebApi.Handlers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace  FplBot.WebApi.Pages.Admin
{
    public class Verified : PageModel
    {
        private readonly IVerifiedEntriesRepository _repo;
        private readonly IVerifiedPLEntriesRepository _plRepo;
        private readonly IGlobalSettingsClient _settings;
        private readonly IMediator _mediator;
        private readonly ILogger<Verified> _logger;
        
        public Verified(IVerifiedEntriesRepository repo, IVerifiedPLEntriesRepository plRepo, IEntryClient entryClient, IGlobalSettingsClient settings, IMediator mediator, ILogger<Verified> logger)
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
            VerifiedEntries = await _repo.GetAllVerifiedEntries();
            VerifiedPLEntries = await _plRepo.GetAllVerifiedPLEntries();
            _logger.LogInformation("All fetched");
        }
        
        public async Task<IActionResult> OnPostSeedEntries()
        {
            await _mediator.Publish(new SeedVerifiedEntries());
            TempData["msg"] += $"List imported!";
            return RedirectToPage("Verified");
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
            TempData["msg"] += $"All deleted";
            return RedirectToPage("Verified");
        }

        public async Task<IActionResult> OnPostUpdateEntry(UpdateEntry model, UpdateAction action)
        {
            if (action == UpdateAction.Remove)
            {
                _logger.LogInformation("Removing single: {entryId}", model.EntryId);
                await _repo.Delete(model.EntryId);
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
            await _mediator.Publish(new UpdateEntryLiveStats());
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
        public string EntryStats { get; set; }
    }

    public enum UpdateAction
    {
        Update,
        Remove
    }
    
}