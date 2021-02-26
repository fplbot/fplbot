using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Search.Models;
using FplBot.Core.Data;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("virtual-leagues")]
    public class VirtualLeaguesController : ControllerBase
    {
        private readonly IVerifiedEntriesRepository _repo;
        private readonly IVerifiedPLEntriesRepository _plRepo;

        public VirtualLeaguesController(IVerifiedEntriesRepository repo, IVerifiedPLEntriesRepository plRepo)
        {
            _repo = repo;
            _plRepo = plRepo;
        }

        [HttpGet("pl")]
        public async Task<IActionResult> GetPL()
        {
            IEnumerable<VerifiedEntry> allVerifiedEntries = await _repo.GetAllVerifiedEntries();
            if (allVerifiedEntries == null || !allVerifiedEntries.Any())
                return Ok(new List<PLVirtualLeagueEntry>());

            allVerifiedEntries = allVerifiedEntries.Where(v => v.VerifiedEntryType == VerifiedEntryType.FootballerInPL);
            
            var plVerifiedEntries = await _plRepo.GetAllVerifiedPLEntries();


            var lastGwOrder = allVerifiedEntries.OrderByDescending(e => e.EntryStats.LastGwTotalPoints).ToList();
            var currentGwOrder = allVerifiedEntries.OrderByDescending(e => e.EntryStats.CurrentGwTotalPoints).ToList();
            
            var viewModels = new List<PLVirtualLeagueEntry>();
            var lastGameweekRank = 0;
            foreach (var item in lastGwOrder)
            {
                var currentRank = currentGwOrder.FindIndex(x => x.EntryId == item.EntryId);
                var currentGwItem = currentGwOrder[currentRank];
                var movement = currentRank - lastGameweekRank;
                var currentPLEntry = plVerifiedEntries.First(c => c.EntryId == item.EntryId);
                viewModels.Add(ApiModelBuilder.BuildPLVirtualLeagueEntry(currentPLEntry, currentGwItem) with { Movement = movement});
                lastGameweekRank++;
            }
            return Ok(viewModels.OrderByDescending(c => c.TotalPoints));
        }


        [HttpGet("")]
        [HttpGet("types/")]
        public async Task<IActionResult> GetLeagues()
        {
            return Ok(Enum.GetNames<VerifiedEntryType>());
        }
        
        [HttpGet("types/{type}")]
        public async Task<IActionResult> GetVirtualLeague(string type)
        {
            var verifiedEntries = await _repo.GetAllVerifiedEntries();
            if (verifiedEntries == null)
                return Ok(Enumerable.Empty<string>());

            if (!string.IsNullOrEmpty(type))
            {
                bool couldParse = Enum.TryParse(type, true, out VerifiedEntryType theType);
                if (couldParse)
                {
                    verifiedEntries = verifiedEntries.Where(v => v.VerifiedEntryType == theType).ToList();    
                }
                else
                {
                    ModelState.AddModelError("type", "Unknown modelType");
                    return BadRequest();
                }
            }

            var lastGwOrder = verifiedEntries.OrderByDescending(e => e.EntryStats.LastGwTotalPoints).ToList();
            var currentGwOrder = verifiedEntries.OrderByDescending(c => c.EntryStats.CurrentGwTotalPoints).ToList();
            
            var lastGameweekRank = 0;
            var viewModels = new List<RegularVirtualLeagueEntry>();
            foreach (var item in lastGwOrder)
            {
                var currentRank = currentGwOrder.FindIndex(x => x.EntryId == item.EntryId);
                var currentGwItem = currentGwOrder[currentRank];
                var movement = currentRank - lastGameweekRank;
                viewModels.Add(ApiModelBuilder.BuildVirtualLeagueEntry(currentGwItem) with{ Movement = movement});
                lastGameweekRank++;
            }
            
            return Ok(viewModels.OrderByDescending(c => c.TotalPoints));
        }
    }
}