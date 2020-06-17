using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Endpoints.Middlewares
{
    public class HttpItemsManager
    {
        private readonly RequestDelegate _next;
        private ILogger<HttpItemsManager> _logger;

        public HttpItemsManager(RequestDelegate next, ILogger<HttpItemsManager> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                var jObject = JObject.Parse(body);

                if (jObject.ContainsKey("challenge"))
                {
                    context.Items.Add(HttpItemKeys.ChallengeKey, jObject["challenge"]);
                }
                else
                {
                    var metadata = JsonConvert.DeserializeObject<EventMetaData>(body);
                    if (jObject["event"] is JObject @event)
                    {
                        var slackEvent = ToEventType(@event);
                        context.Items.Add(HttpItemKeys.EventMetadataKey, metadata);
                        context.Items.Add(HttpItemKeys.SlackEventKey, slackEvent);
                        context.Items.Add(HttpItemKeys.EventTypeKey, @event["type"]);
                    }
                }
                context.Request.Body.Position = 0;
            }

            await _next(context);
        }

        private static SlackEvent ToEventType(JObject eventJson)
        {
            var eventType = GetEventType(eventJson);
            switch (eventType)
            {    
                case EventTypes.AppMention:
                    return eventJson.ToObject<AppMentionEvent>();
                default:
                    return eventJson.ToObject<SlackEvent>();
            }
        }
        
        public static string GetEventType(JObject eventJson)
        {
            if (eventJson != null)
            {
                return eventJson["type"].Value<string>();
            }
            
            return "unknown";
        }
    }
}