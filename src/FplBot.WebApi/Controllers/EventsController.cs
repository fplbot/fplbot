using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FplBot.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly IHandleAllEvents _responseHandler;

        public EventsController(ILogger<EventsController> logger, IHandleAllEvents responseHandler)
        {
            _logger = logger;
            _responseHandler = responseHandler;
        }
        
        [HttpPost]

        public async Task<ActionResult> Index([FromBody]EventWrapper eventWrapper)
        {
            _logger.LogInformation(eventWrapper.Challenge);

            if (!string.IsNullOrEmpty(eventWrapper.Challenge))
            {
                return new JsonResult(new {challenge = eventWrapper.Challenge});
            }

            if (eventWrapper.Event != null)
            {
                await _responseHandler.Handle(eventWrapper);
                return Ok();
            }

            _logger.LogError("No payload");
            return BadRequest(new {error = "No event property on payload"});
        }
    }
    
    public class EventWrapper
    {
        public string Token { get; set; }
        public string Team_Id { get; set; }
        public BotMentionedEvent Event { get; set; }
        public string Type { get; set; }
        public string[] AuthedUsers { get; set; }
        public string Event_Id { get; set; }
        public long Event_Time { get; set; }
        // only used for install-verify
        public string Challenge { get; set; }
    }

    public class BotMentionedEvent
    {
        public string Text { get; set; }
        public string Channel { get; set; }
        public string Type { get; set; }
        public string Ts { get; set; }
        public string Event_Ts { get; set; }
    }
}