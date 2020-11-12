using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Middlewares
{
    internal class AppHomeOpenedEvents
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AppHomeOpenedEvents> _logger;
        private readonly IEnumerable<IHandleAppHomeOpened> _responseHandlers;

        public AppHomeOpenedEvents(RequestDelegate next, ILogger<AppHomeOpenedEvents> logger, IEnumerable<IHandleAppHomeOpened> responseHandlers)
        {
            _next = next;
            _logger = logger;
            _responseHandlers = responseHandlers;
        }

        public async Task Invoke(HttpContext context)
        {
            var appHomeOpenedEvent = (AppHomeOpenedEvent) context.Items[HttpItemKeys.SlackEventKey];
            var metadata = (EventMetaData) context.Items[HttpItemKeys.EventMetadataKey];
            var handler = _responseHandlers.FirstOrDefault();
            
            if (handler == null)
            {
                _logger.LogError("No handler registered for IHandleAppHomeOpened");
            }
            else
            {
                _logger.LogInformation($"Handling using {handler.GetType()}");
                try
                {
                    _logger.LogInformation($"Handling using {handler.GetType()}");
                    var response = await handler.Handle(metadata,appHomeOpenedEvent);
                    _logger.LogInformation(response.Response);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }

            await _next(context);
        }

        public static bool ShouldRun(HttpContext ctx) =>  ctx.Items.ContainsKey(HttpItemKeys.EventTypeKey) && (ctx.Items[HttpItemKeys.EventTypeKey].ToString() == EventTypes.AppHomeOpened);

    }
}