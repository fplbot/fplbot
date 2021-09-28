using System;
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

        public OAuthController(ILogger<OAuthController> logger, IOptions<OAuthOptions> options)
        {
            _logger = logger;
            _options = options;
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
    }
}
