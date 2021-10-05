using System;
using Discord.Net.Endpoints.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Hosting;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly ILogger<OAuthController> _logger;
        private readonly IOptions<OAuthOptions> _options;
        private readonly IOptions<DiscordOAuthOptions> _discordOptions;

        public OAuthController(ILogger<OAuthController> logger, IOptions<OAuthOptions> options, IOptions<DiscordOAuthOptions> discordOptions)
        {
            _logger = logger;
            _options = options;
            _discordOptions = discordOptions;
        }


        [HttpGet("install-url")]
        public IActionResult InstallUrl()
        {
            _logger.LogInformation($"Installing");
            var original = new Uri(HttpContext.Request.GetDisplayUrl());
            var redirect_uri = new Uri(original, "/oauth/authorize");
            return Ok(new {
                redirectUri = $"https://slack.com/oauth/v2/authorize?&user_scope=&scope=app_mentions:read,chat:write,chat:write.customize,chat:write.public,users.profile:read,users:read,users:read.email,groups:read,channels:read&client_id={_options.Value.CLIENT_ID}&redirect_uri={redirect_uri}"
            });
        }

        [HttpGet("install-url-discord")]
        public IActionResult InstallUrlDiscord()
        {
            _logger.LogInformation($"Installing");
            var original = new Uri(HttpContext.Request.GetDisplayUrl());
            var redirectUri = new Uri(original, "/oauth/discord/authorize");
            return Ok(new {
                redirectUri = $"https://discord.com/api/oauth2/authorize?client_id={_discordOptions.Value.CLIENT_ID}&redirect_uri={redirectUri}&scope=bot%20applications.commands&permissions=309237844032&response_type=code"
            });
        }
    }
}
