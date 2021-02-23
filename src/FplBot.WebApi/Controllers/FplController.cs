using Fpl.Client.Abstractions;
using Fpl.Search;
using FplBot.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Search.Models;
using FplBot.Core.Data;
using VerifiedPLEntry = FplBot.Core.Data.VerifiedPLEntry;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FplController : ControllerBase
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IVerifiedEntriesRepository _repo;
        private readonly IVerifiedPLEntriesRepository _plRepo;
        private readonly IVerifiedEntriesService _verifiedEntriesService;
        private readonly ILogger<FplController> _logger;

        public FplController(ILeagueClient leagueClient, IVerifiedEntriesRepository repo, IVerifiedPLEntriesRepository plRepo, IVerifiedEntriesService verifiedEntriesService, ILogger<FplController> logger)
        {
            _leagueClient = leagueClient;
            _repo = repo;
            _plRepo = plRepo;
            _verifiedEntriesService = verifiedEntriesService;
            _logger = logger;
        }
        
        [HttpGet("leagues/{leagueId}")]
        public async Task<IActionResult> GetLeague(int leagueId)
        {
            try
            {
                var league = await _leagueClient.GetClassicLeague(leagueId);
                return Ok(new
                {
                    LeagueName = league.Properties.Name,
                    LeagueAdmin = league.Standings.Entries.FirstOrDefault(e => e.Entry == league.Properties.AdminEntry)?.PlayerName
                });
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }

            return NotFound();
        }

        [HttpGet("verified")]
        public async Task<IActionResult> GetVerifiedEntries()
        {
            try
            {
                var verifiedEntries = await _verifiedEntriesService.GetAllVerifiedPLEntries();
                return Ok(verifiedEntries);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }

            return NotFound();
        }

        [HttpGet("verified/{slug}")]
        public async Task<IActionResult> GetVerifiedEntry(string slug)
        {
            try
            {
                var slugEntry = VerifiedEntries.VerifiedPLEntries.FirstOrDefault(x => x.Slug == slug);
                if (slugEntry == null)
                {
                    return NotFound();
                }
                var verifiedEntry = await _verifiedEntriesService.GetVerifiedPLEntry(slugEntry.Slug);
                if (verifiedEntry == null)
                {
                    return NotFound();
                }

                return Ok(verifiedEntry);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }

            return NotFound();
        }

        [HttpGet("v2/pl-verified")]
        public async Task<IActionResult> GetPLVerified()
        {
            IEnumerable<VerifiedEntry> allVerifiedEntries = await _repo.GetAllVerifiedEntries();
            if (allVerifiedEntries == null || !allVerifiedEntries.Any())
                return Ok(new List<VerifiedPLEntryModelV2>());

            allVerifiedEntries = allVerifiedEntries.Where(v => v.VerifiedEntryType == VerifiedEntryType.FootballerInPL);
            
            var plVerifiedEntries = await _plRepo.GetAllVerifiedPLEntries();


            var lastGwOrder = allVerifiedEntries.OrderByDescending(e => e.EntryStats.LastGwTotalPoints).ToList();
            var currentGwOrder = allVerifiedEntries.OrderByDescending(e => e.EntryStats.CurrentGwTotalPoints).ToList();
            
            var viewModels = new List<VerifiedPLEntryModelV2>();
            var lastGameweekRank = 0;
            foreach (var item in lastGwOrder)
            {
                var currentRank = currentGwOrder.FindIndex(x => x.EntryId == item.EntryId);
                var currentGwItem = currentGwOrder[currentRank];
                var movement = currentRank - lastGameweekRank;
                var currentPLEntry = plVerifiedEntries.First(c => c.EntryId == item.EntryId);
                viewModels.Add(BuildVerifiedPlEntryModelV2(currentPLEntry, currentGwItem) with { Movement = movement});
                lastGameweekRank++;
            }
            return Ok(viewModels.OrderByDescending(c => c.TotalPoints));
        }

        [HttpGet("v2/pl-verified/{entryId}")]
        public async Task<IActionResult> GetPLVerifiedByEntryId(int entryId)
        {
            try
            {
                var plVerifiedEntry = await _plRepo.GetVerifiedPLEntry(entryId);
                if (plVerifiedEntry == null)
                    return NotFound();

                var verifiedEntry = await _repo.GetVerifiedEntry(entryId);
                if (verifiedEntry == null)
                    return NotFound();
                
                return Ok(BuildVerifiedPlEntryModelV2(plVerifiedEntry, verifiedEntry));
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }
            return NotFound();
        }

        [HttpGet("v2/verified")]
        public async Task<IActionResult> GetVerifiedV2()
        {
            try
            {
                var verifiedEntries = await _repo.GetAllVerifiedEntries();
                if (verifiedEntries == null)
                    return NotFound();
                
                var lastGwOrder = verifiedEntries.OrderByDescending(e => e.EntryStats.LastGwTotalPoints).ToList();
                var currentGwOrder = verifiedEntries.OrderByDescending(c => c.EntryStats.CurrentGwTotalPoints).ToList();
                
                var lastGameweekRank = 0;
                var viewModels = new List<VerifiedEntryModelV2>();
                foreach (var item in lastGwOrder)
                {
                    var currentRank = currentGwOrder.FindIndex(x => x.EntryId == item.EntryId);
                    var currentGwItem = currentGwOrder[currentRank];
                    var movement = currentRank - lastGameweekRank;
                    viewModels.Add(BuildVerifiedEntryModelV2(currentGwItem) with{ Movement = movement});
                    lastGameweekRank++;
                }
                
                return Ok(viewModels.OrderByDescending(c => c.TotalPoints));
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }
            return NotFound();
        }

        [HttpGet("v2/verified/{entryId}")]
        public async Task<IActionResult> GetVerifiedByEntryId(int entryId)
        {
            try
            {
                var verifiedEntry = await _repo.GetVerifiedEntry(entryId);
                if (verifiedEntry == null)
                    return NotFound();
                
                return Ok(verifiedEntry);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }
            return NotFound();
        }

        private static VerifiedPLEntryModelV2 BuildVerifiedPlEntryModelV2(VerifiedPLEntry plEntry, VerifiedEntry fplEntry)
        {
            var baseModel = BuildVerifiedEntryModelV2(fplEntry);
            var plModel = new VerifiedPLEntryModelV2
            (
                EntryId : fplEntry.EntryId,
                Slug: plEntry.PlayerFullName.ToLower().Replace(" ", "-"),
                TeamName : fplEntry.EntryTeamName,
                RealName : fplEntry.FullName,
                PointsThisGw: fplEntry.EntryStats?.PointsThisGw,
                TotalPoints: fplEntry.EntryStats?.CurrentGwTotalPoints,
                OverallRank: fplEntry.EntryStats?.OverallRank,
                Movement:0,
                Captain: fplEntry.EntryStats?.Captain,
                ViceCaptain: fplEntry.EntryStats?.ViceCaptain,
                ChipUsed: fplEntry.EntryStats?.ActiveChip,
                Gameweek: fplEntry.EntryStats?.Gameweek,
                PLPlayerId: plEntry.PlayerId,
                PLName : plEntry.PlayerFullName,
                PlaysForTeam : plEntry.TeamName,
                ShirtImageUrl : $"https://fantasy.premierleague.com/dist/img/shirts/standard/shirt_{plEntry.TeamCode}-66.png", // move url-gen to client ?
                ImageUrl : $"https://resources.premierleague.com/premierleague/photos/players/110x140/p{plEntry.PlayerCode}.png", // move url-gen to client ?
                SelfOwnershipWeekCount : plEntry.SelfOwnershipStats.WeekCount,
                SelfOwnershipTotalPoints : plEntry.SelfOwnershipStats.TotalPoints
            );
            return plModel;
        }

        private static VerifiedEntryModelV2 BuildVerifiedEntryModelV2(VerifiedEntry fplEntry)
        {
            return new VerifiedEntryModelV2(
                EntryId : fplEntry.EntryId,
                TeamName : fplEntry.EntryTeamName,
                RealName : fplEntry.FullName,
                PointsThisGw: fplEntry.EntryStats?.PointsThisGw,
                TotalPoints: fplEntry.EntryStats?.CurrentGwTotalPoints,
                OverallRank: fplEntry.EntryStats?.OverallRank,
                Movement:0,
                Captain: fplEntry.EntryStats?.Captain,
                ViceCaptain: fplEntry.EntryStats?.ViceCaptain,
                ChipUsed: fplEntry.EntryStats?.ActiveChip,
                Gameweek: fplEntry.EntryStats?.Gameweek
                );
        }
    }
    
    
    public record VerifiedPLEntryModelV2(
        int EntryId,
        string Slug,
        string TeamName,
        string RealName,
        int? PLPlayerId,
        string PLName,
        string PlaysForTeam,
        string ShirtImageUrl,
        string ImageUrl,
        int? PointsThisGw,
        int? TotalPoints,
        int? OverallRank,
        int Movement,
        string Captain,
        string ViceCaptain,
        string ChipUsed,
        int SelfOwnershipWeekCount,
        int SelfOwnershipTotalPoints,
        int? Gameweek
    );
    
    
    public record VerifiedEntryModelV2(
        int EntryId,
        string TeamName,
        string RealName,
        int? PointsThisGw,
        int? TotalPoints,
        int? OverallRank,
        int Movement,
        string Captain,
        string ViceCaptain,
        string ChipUsed,
        int? Gameweek
        );

    
}