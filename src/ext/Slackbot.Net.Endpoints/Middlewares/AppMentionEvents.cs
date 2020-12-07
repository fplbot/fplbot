using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Endpoints.Middlewares
{
    public class AppMentionEvents
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AppMentionEvents> _logger;
        
        private readonly ISelectAppMentionEventHandlers _responseHandler;

        public AppMentionEvents(RequestDelegate next, ILogger<AppMentionEvents> logger, ISelectAppMentionEventHandlers responseHandler)
        {
            _next = next;
            _logger = logger;
            _responseHandler = responseHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            var metadata = (EventMetaData) context.Items[HttpItemKeys.EventMetadataKey];
            var appMentionEvent = (AppMentionEvent) context.Items[HttpItemKeys.SlackEventKey];
            var handlers = await _responseHandler.GetAppMentionEventHandlerFor(metadata, appMentionEvent);

            _logger.BeginScope(new Dictionary<string, object>
            {
                ["Slack_TeamId"] = metadata?.Team_Id,
                ["Slack_Channel"] = appMentionEvent?.Channel,
                ["Slack_User"] = appMentionEvent?.User,
            });
            foreach (var handler in handlers)
            {
                try
                {
                    _logger.LogInformation($"Handling using {handler.GetType()}");
                    await handler.Handle(metadata, appMentionEvent);
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
            return ctx.Items.ContainsKey(HttpItemKeys.EventTypeKey) && (ctx.Items[HttpItemKeys.EventTypeKey].ToString() == EventTypes.AppMention);
        }
    }
}