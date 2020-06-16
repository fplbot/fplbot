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
        private readonly ILogger<SlackEventsMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly IHandleAllEvents _responseHandler;

        public SlackEventsMiddleware(RequestDelegate next, ILogger<SlackEventsMiddleware> logger, IHandleAllEvents responseHandler)
        {
            _next = next;
            _logger = logger;
            _responseHandler = responseHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                var jObject = JObject.Parse(body);
                var metadata = JsonConvert.DeserializeObject<EventMetaData>(body);
                var @event = jObject["event"] as JObject;
                await _responseHandler.Handle(metadata, @event);
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