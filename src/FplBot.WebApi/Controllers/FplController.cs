using Fpl.Client.Abstractions;
using Fpl.Search;
using FplBot.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly ILogger<FplController> _logger;

        public FplController(ILeagueClient leagueClient, IVerifiedEntriesService verifiedEntriesService, ILogger<FplController> logger)
        {
            _leagueClient = leagueClient;
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
                var slugEntry = VerifiedEntries.VerifiedPLEntries.FirstOrDefault(x => x.Slug != slug);
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
    }
}