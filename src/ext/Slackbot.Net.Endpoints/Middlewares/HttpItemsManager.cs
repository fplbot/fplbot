using System.IO;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Endpoints.Models.Interactive;
using Slackbot.Net.Endpoints.Models.Interactive.ViewSubmissions;

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

                if (body.StartsWith("{"))
                {
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
                            var slackEvent = ToEventType(@event, body);
                            context.Items.Add(HttpItemKeys.EventMetadataKey, metadata);
                            context.Items.Add(HttpItemKeys.SlackEventKey, slackEvent);
                            context.Items.Add(HttpItemKeys.EventTypeKey, @event["type"]);
                        }
                    }
                }
                // https://api.slack.com/interactivity/handling#payloads
                // The body of that request will contain a payload parameter.
                // Your app should parse this payload parameter as JSON.
                else if (body.StartsWith("payload="))
                {
                    _logger.LogTrace(body);
                    var payloadJsonUrlEncoded = body.Remove(0,8);
                    var decodedJson = System.Net.WebUtility.UrlDecode(payloadJsonUrlEncoded);
                    var payload = JObject.Parse(decodedJson);
                    var interactivePayloadTyped = ToInteractiveType(payload, body);
                    context.Items.Add(HttpItemKeys.InteractivePayloadKey, interactivePayloadTyped);
                }
 
                context.Request.Body.Position = 0;
            }

            await _next(context);
        }

        private static SlackEvent ToEventType(JObject eventJson, string raw)
        {
            var eventType = GetEventType(eventJson);
            switch (eventType)
            {
                case EventTypes.AppMention:
                    return eventJson.ToObject<AppMentionEvent>();
                case EventTypes.MemberJoinedChannel:
                    return eventJson.ToObject<MemberJoinedChannelEvent>();
                case EventTypes.AppHomeOpened:
                    return eventJson.ToObject<AppHomeOpenedEvent>();
                default:
                    UnknownSlackEvent unknownSlackEvent = eventJson.ToObject<UnknownSlackEvent>();
                    unknownSlackEvent.RawJson = raw;
                    return unknownSlackEvent;
            }
        }
        
        private static Interaction ToInteractiveType(JObject payloadJson, string raw)
        {
            var eventType = GetEventType(payloadJson);
            switch (eventType)
            {
                case InteractionTypes.ViewSubmission:
                    var viewSubmission = payloadJson.ToObject<ViewSubmission>();
                    
                    var view = payloadJson["view"] as JObject;
                    var viewState = view["state"] as JObject;;
                    viewSubmission.ViewId = view.Value<string>("id");
                    viewSubmission.ViewState = viewState;
                    return viewSubmission;
                default:
                    var unknownSlackEvent = payloadJson.ToObject<UnknownInteractiveMessage>();
                    unknownSlackEvent.RawJson = raw;
                    return unknownSlackEvent;
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