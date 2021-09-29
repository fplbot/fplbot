using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.Endpoints.OAuth;

namespace Slackbot.Net.Endpoints.Middlewares
{
    internal class SlackbotCodeTokenExchangeMiddleware
    {
        private readonly RequestDelegate _next;

        public SlackbotCodeTokenExchangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext ctx, OAuthClient oAuthAccessClient, IServiceProvider provider, IOptions<OAuthOptions> options, ITokenStore slackTeamRepository, ILogger<SlackbotCodeTokenExchangeMiddleware> logger)
        {
            logger.LogInformation("Installing!");
            var redirect_uri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host.Value.ToString()}{ctx.Request.PathBase.Value}");
            var code = ctx.Request.Query["code"].FirstOrDefault();

            if(code == null)
                await _next(ctx);

            var response = await oAuthAccessClient.OAuthAccessV2(new OAuthClient.OauthAccessV2Request(
                code,
                options.Value.CLIENT_ID,
                options.Value.CLIENT_SECRET,
                redirect_uri.ToString()
            ));

            if (response.Ok)
            {
                logger.LogInformation($"Oauth response! ok:{response.Ok}");
                await slackTeamRepository.Insert(new Workspace
                (
                    response.Team.Id,
                    response.Team.Name,
                    response.Access_Token
                ));
                await options.Value.OnSuccess(response.Team.Id, response.Team.Name, provider);

                ctx.Response.Redirect(options.Value.SuccessRedirectUri);
            }
            else
            {
                logger.LogError($"Bad Oauth response! {response}");
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsync(response.Error);
            }
        }
    }
}
