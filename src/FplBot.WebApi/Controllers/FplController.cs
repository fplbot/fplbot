using Fpl.Client.Abstractions;
using FplBot.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public async Task<IActionResult> GetVerifiedLeague()
        {
            try
            {
                var league = await _verifiedEntriesService.GetAllVerifiedPLEntries();
                return Ok(league);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }

            return NotFound();
        }

        [HttpGet("verified/{slug}")]
        public async Task<IActionResult> GetVerifiedTeam(string slug)
        {
            try
            {
                var team = await _verifiedEntriesService.GetVerifiedPLEntry(slug);

                if (team == null)
                {
                    return NotFound();
                }

                return Ok(team);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }

            return NotFound();
        }
    }
}