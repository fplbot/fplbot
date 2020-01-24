using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.OAuthAccess;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly ILogger<OAuthController> _logger;
        private readonly ISlackOAuthAccessClient _oAuthAccessClient;
        private readonly ISlackTeamRepository _slackTeamRepository;
        private readonly IOptions<DistributedSlackAppOptions> _options;

        public OAuthController(ILogger<OAuthController> logger, ISlackOAuthAccessClient oAuthAccessClient, ISlackTeamRepository slackTeamRepository, IOptions<DistributedSlackAppOptions> options)
        {
            _logger = logger;
            _oAuthAccessClient = oAuthAccessClient;
            _slackTeamRepository = slackTeamRepository;
            _options = options;
        }

        [HttpGet("install")]
        public IActionResult Install()
        {
            _logger.LogInformation("Installing!");
            return Redirect($"https://slack.com/oauth/authorize?scope=bot,chat:write:bot&client_id={_options.Value.CLIENT_ID}");
        }

        [HttpGet("uninstall")]
        public async Task<IActionResult> Uninstall(string teamId)
        {
            await _slackTeamRepository.Delete(teamId);
            return Ok("Ok");
        }

        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize(string code, string state)
        {
            _logger.LogInformation("Authorizing!");
            var response = await _oAuthAccessClient.OAuthAccess(new OauthAccessRequest
            {
                ClientId = _options.Value.CLIENT_ID, 
                ClientSecret = _options.Value.CLIENT_SECRET, 
                Code = code, 
                SingleChannel = true
            });

            if (response.Ok)
            {
                _logger.LogInformation($"Oauth response! {response.Ok}");
            
                await _slackTeamRepository.Insert(new SlackTeam
                {
                    TeamId = response.Team_Id,
                    TeamName = response.Team_Name,
                    Scope = response.Scope,
                    AccessToken = response.Access_Token,
                    FplBotSlackChannel = state
                });

                return Ok(); 
            }
            _logger.LogInformation($"Oauth response not ok! {response.Error}");
            return BadRequest(response.Error);
        }
        
        
    }
}
