using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.OAuthAccess;
using StackExchange.Redis;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuthController : ControllerBase
    {

        private readonly ILogger<OAuthController> _logger;
        private readonly ISlackOAuthAccessClient _oAuthAccessClient;
        private readonly ConnectionMultiplexer _redis;

        private static readonly string CLIENT_ID = Environment.GetEnvironmentVariable("CLIENT_ID");
        private static readonly string CLIENT_SECRET = Environment.GetEnvironmentVariable("CLIENT_SECRET");

        public OAuthController(ILogger<OAuthController> logger, ISlackOAuthAccessClient oAuthAccessClient, ConnectionMultiplexer redis)
        {
            _logger = logger;
            _oAuthAccessClient = oAuthAccessClient;
            _redis = redis;
        }

        [HttpGet("install")]
        public IActionResult Install()
        {
            return Redirect($"https://slack.com/oauth/authorize?scope=bot,chat:write:bot&client_id={CLIENT_ID}");
        }

        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize(string code)
        {
            var response = await _oAuthAccessClient.OAuthAccess(new OauthAccessRequest
            {
                ClientId = CLIENT_ID, ClientSecret = CLIENT_SECRET, Code = code, SingleChannel = true
            });
            IDatabase db = _redis.GetDatabase();
            db.HashSet(response.Team_Id, "TeamName", response.Team_Name);
            db.HashSet(response.Team_Id, "Scope", response.Scope);
            db.HashSet(response.Team_Id, "AccessToken", response.Access_Token);

            return Ok();
        }
    }
}
