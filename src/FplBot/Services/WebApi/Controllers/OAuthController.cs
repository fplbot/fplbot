using Discord.Net.Endpoints.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Hosting;

namespace FplBot.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class OAuthController(
    ILogger<OAuthController> logger,
    IOptions<OAuthOptions> options,
    IOptions<DiscordOAuthOptions> discordOptions)
    : ControllerBase
{
    [HttpGet("install-url")]
    public IActionResult InstallUrl()
    {
        logger.LogInformation($"Installing");
        var original = new Uri(HttpContext.Request.GetDisplayUrl());
        var redirect_uri = new Uri(original, "/oauth/authorize");
        return Ok(new {
            redirectUri = $"https://slack.com/oauth/v2/authorize?&user_scope=&scope=app_mentions:read,chat:write,chat:write.customize,chat:write.public,users.profile:read,users:read,users:read.email,groups:read,channels:read&client_id={options.Value.CLIENT_ID}&redirect_uri={redirect_uri}"
        });
    }

    [HttpGet("install-url-discord")]
    public IActionResult InstallUrlDiscord()
    {
        logger.LogInformation($"Installing");
        var original = new Uri(HttpContext.Request.GetDisplayUrl());
        var redirectUri = new Uri(original, "/oauth/discord/authorize");
        return Ok(new {
            redirectUri = $"https://discord.com/api/oauth2/authorize?client_id={discordOptions.Value.CLIENT_ID}&redirect_uri={redirectUri}&scope=bot%20applications.commands&permissions=309237844032&response_type=code"
        });
    }
}
