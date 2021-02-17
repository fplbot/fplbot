using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FplController : ControllerBase
    {
        private readonly ILeagueClient _leagueClient;
        private readonly IVerifiedLeagueService _verifiedLeagueService;
        private readonly ILogger<FplController> _logger;

        public FplController(ILeagueClient leagueClient, IVerifiedLeagueService verifiedLeagueService, ILogger<FplController> logger)
        {
            _leagueClient = leagueClient;
            _verifiedLeagueService = verifiedLeagueService;
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
                var league = await _verifiedLeagueService.GetStandings();
                return Ok(league);
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }

            return NotFound();
        }
    }
}