using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public class Events
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<Events> _logger;
        
        private readonly ISelectEventHandlers _responseHandler;

        public Events(RequestDelegate next, ILogger<Events> logger, ISelectEventHandlers responseHandler)
        {
            _next = next;
            _logger = logger;
            _responseHandler = responseHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            var metadata = (EventMetaData) context.Items[HttpItemKeys.EventMetadataKey];
            var slackEvent = (SlackEvent) context.Items[HttpItemKeys.SlackEventKey];
            var handlers = await _responseHandler.GetEventHandlerFor(metadata, slackEvent);
            
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(metadata, slackEvent);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
            context.Response.StatusCode = 200;
        }

        public static bool ShouldRun(HttpContext ctx)
        {
            return ctx.Items.ContainsKey(HttpItemKeys.EventTypeKey) && (ctx.Items[HttpItemKeys.EventTypeKey].ToString() != EventTypes.AppUninstalled &&
                                                                         ctx.Items[HttpItemKeys.EventTypeKey].ToString() != EventTypes.TokensRevoked);
        }
    }
}