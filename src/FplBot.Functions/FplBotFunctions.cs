using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FplBot.Functions
{
    public class FplBotFunctions
    {
        [Function("Pong")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger<FplBotFunctions>();
            logger.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Url.Query;
            var r = req.CreateResponse(HttpStatusCode.OK);
            await r.WriteStringAsync($"Hello, {name}");
            return r;
        }
    }
}
