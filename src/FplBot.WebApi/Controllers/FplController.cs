using Fpl.Client.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FplBot.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class FplController : ControllerBase
{
    private readonly ILeagueClient _leagueClient;
    private readonly ILogger<FplController> _logger;

    public FplController(ILeagueClient leagueClient, ILogger<FplController> logger)
    {
        _leagueClient = leagueClient;
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
}
