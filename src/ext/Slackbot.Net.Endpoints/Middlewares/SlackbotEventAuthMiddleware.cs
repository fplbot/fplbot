using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Authentication;

namespace Slackbot.Net.Endpoints.Middlewares
{
    internal class SlackbotEventAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public SlackbotEventAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext ctx, ILogger<SlackbotEventAuthMiddleware> logger)
        {
            bool success = false;
            try
            {
                var res = await ctx.AuthenticateAsync(SlackbotEventsAuthenticationConstants.AuthenticationScheme);
                success = res.Succeeded;
            }
            catch (InvalidOperationException ioe)
            {
                throw new InvalidOperationException("Did you forget to call AddAuthentication().AddSlackbotEvents()", ioe);
            }

            if (success)
            {
                await _next(ctx);
            }
            else
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("UNAUTHORIZED");
            }
        }
    }
}
