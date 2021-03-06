using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Data;
using Fpl.Data.Models;
using Fpl.Data.Repositories;
using Fpl.Search.Models;

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


            var lastGwOrder = allVerifiedEntries.OrderByDescending(e => e.EntryStats.LastGwTotalPoints).ThenBy(o => o.EntryStats.OverallRank).ToList();
            var currentGwOrder = allVerifiedEntries.OrderByDescending(e => e.EntryStats.CurrentGwTotalPoints).ThenBy(o => o.EntryStats.OverallRank).ToList();
            
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
            return Ok(viewModels.OrderByDescending(c => c.TotalPoints).ThenBy(o => o.OverallRank));
        }


        [HttpGet("")]
        public IActionResult GetLeagues()
        {
            return Ok(Enum.GetNames<VerifiedEntryType>());
        }
        
        [HttpGet("league")]
        public async Task<IActionResult> GetVirtualLeague()
        {
            var type = Request.Query["type"];
            var verifiedEntries = await _repo.GetAllVerifiedEntries();
            if (verifiedEntries == null)
                return Ok(Enumerable.Empty<string>());

            if (type.Count > 0 && !type.ToList().Any(t => string.Compare("all", t, StringComparison.InvariantCultureIgnoreCase) == 0))
            {
                bool couldParse = true;
                var filteredTypes = new List<VerifiedEntryType>();
                foreach (string typeSing in type)
                {
                    VerifiedEntryType theType = VerifiedEntryType.Unknown;
                    couldParse = couldParse && Enum.TryParse(typeSing, true, out theType);
                    if(couldParse)
                        filteredTypes.Add(theType);   
                }
                
                if (!couldParse)
                {
                    ModelState.AddModelError("type", "Unknown modelType");
                    return BadRequest();
                }

                verifiedEntries = verifiedEntries.Where(v => filteredTypes.Any(f => f == v.VerifiedEntryType)).ToList();
            }

            var lastGwOrder = verifiedEntries.OrderByDescending(e => e.EntryStats.LastGwTotalPoints).ThenBy(o => o.EntryStats.OverallRank).ToList();
            var currentGwOrder = verifiedEntries.OrderByDescending(c => c.EntryStats.CurrentGwTotalPoints).ThenBy(o => o.EntryStats.OverallRank).ToList();
            
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
            
            return Ok(viewModels.OrderByDescending(c => c.TotalPoints).ThenBy(o => o.OverallRank));
        }
    }
}