using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.OAuthAccess;
using Slackbot.Net.SlackClients.Http.Models.Responses.OAuthAccess;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuthController : ControllerBase
    {

        private readonly ILogger<OAuthController> _logger;
        private readonly ISlackOAuthAccessClient _oAuthAccessClient;

        private static readonly string CLIENT_ID = "10330912275.864533206375";

        public OAuthController(ILogger<OAuthController> logger, ISlackOAuthAccessClient oAuthAccessClient)
        {
            _logger = logger;
            _oAuthAccessClient = oAuthAccessClient;
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
                ClientId = CLIENT_ID, ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET"), Code = code, SingleChannel = true
            });

            Store.Responses.Add(response.Team_Id, response);

            return Ok();
        }
        
        [HttpGet("log")]
        public void Log()
        {
            foreach (var responses in Store.Responses)
            {
                _logger.LogInformation($"{responses.Key} - {responses.Value.Bot.Bot_User_Id}");
            }
        }
    }

    public static class Store
    {
        public static Dictionary<string, OAuthAccessResponse> Responses = new Dictionary<string, OAuthAccessResponse>();
    }
}
