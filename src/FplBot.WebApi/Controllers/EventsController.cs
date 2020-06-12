using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FplBot.WebApi.Controllers
{
    [Route("[controller]")]
    public class EventsController : Controller
    {
        private readonly ILogger<EventsController> _logger;
        private readonly IHandleAllEvents _responseHandler;

        public EventsController(ILogger<EventsController> logger, IHandleAllEvents responseHandler)
        {
            _logger = logger;
            _responseHandler = responseHandler;
        }
        
        public async Task<ActionResult> Index([FromBody]EventWrapper eventWrapper)
        {
            _logger.LogInformation(eventWrapper.Challenge);

            if (!string.IsNullOrEmpty(eventWrapper.Challenge))
            {
                return new JsonResult(new {challenge = eventWrapper.Challenge});
            }

            if (!string.IsNullOrEmpty(eventWrapper.Event))
            {
                await _responseHandler.Handle(@eventWrapper.Event);
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
        public string Event { get; set; }
        public string Type { get; set; }
        public string[] AuthedUsers { get; set; }
        public string Event_Id { get; set; }
        public string Event_Time { get; set; }
        // only used for install-verify
        public string Challenge { get; set; }
    }
}