using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public class SlackEventsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SlackEventsMiddleware> _logger;
        
        private readonly ISelectEventHandlers _responseHandler;

        public SlackEventsMiddleware(RequestDelegate next, ILogger<SlackEventsMiddleware> logger, ISelectEventHandlers responseHandler)
        {
            _next = next;
            _logger = logger;
            _responseHandler = responseHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                var jObject = JObject.Parse(body);
                var metadata = JsonConvert.DeserializeObject<EventMetaData>(body);
                var @event = jObject["event"] as JObject;
                var slackEvent = @event.ToObject<SlackEvent>();
                var handlers = await _responseHandler.GetEventHandlerFor(metadata, @slackEvent);
                
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
                context.Request.Body.Position = 0;
            }

            context.Response.StatusCode = 200;
        }

        public static bool ShouldRun(HttpContext cx, string path)
        {
            var isPostForPath = cx.Request.Method == "POST" && cx.Request.Path == path;
            return isPostForPath && cx.Request.Form.ContainsKey("event");
        }
    }
}