using System;
using System.Text.Json;
using System.Threading.Tasks;
using FplBot.WebApi.EventApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FplBot.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly RtmBridgeEventsHandler _responseHandler;

        public EventsController(ILogger<EventsController> logger, RtmBridgeEventsHandler responseHandler)
        {
            _logger = logger;
            _responseHandler = responseHandler;
        }
        
        [HttpPost]
        public async Task<ActionResult> Index([FromBody] JsonElement jsonElement)
        {
            var body = jsonElement.GetRawText();
            var jObject = JObject.Parse(body);
            
            if (jObject.ContainsKey("challenge"))
            {
                return new JsonResult(new {challenge = jObject["challenge"]});
            }

            if (jObject["event"] != null)
            {
                var slackEvent = EventParser.ToEventType(jObject["event"] as JObject);
                await _responseHandler.Handle(jObject.ToObject<EventMetaData>(), slackEvent);
                return Ok();
            }

            _logger.LogError("No payload");
            return BadRequest(new {error = "No event property on payload"});
        }
    }
}