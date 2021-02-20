using Fpl.Client.Abstractions;
using Fpl.Search;
using FplBot.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FplController : ControllerBase
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IVerifiedEntriesService _verifiedEntriesService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FplController> _logger;

        public FplController(ILeagueClient leagueClient, IVerifiedEntriesService verifiedEntriesService, IMemoryCache cache, ILogger<FplController> logger)
        {
            _leagueClient = leagueClient;
            _verifiedEntriesService = verifiedEntriesService;
            _cache = cache;
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
                const string cacheKey = "verifiedentries";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<VerifiedPLEntryModel> verifiedEntries))
                {
                    return Ok(verifiedEntries);
                }

                verifiedEntries = await _verifiedEntriesService.GetAllVerifiedPLEntries();
                _cache.Set(cacheKey, verifiedEntries, TimeSpan.FromMinutes(5));

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
                if (VerifiedEntries.VerifiedPLEntries.All(x => x.Slug != slug))
                {
                    return NotFound();
                }

                var cacheKey = $"verifiedentry-{slug}";
                if (!_cache.TryGetValue(cacheKey, out VerifiedPLEntryModel verifiedEntry))
                {
                    verifiedEntry = await _verifiedEntriesService.GetVerifiedPLEntry(slug);
                    _cache.Set(cacheKey, verifiedEntry, TimeSpan.FromMinutes(5));
                }

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
    }
}