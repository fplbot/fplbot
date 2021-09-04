using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FplBot.Functions.Messaging.Internal;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Functions
{
    public class FplBotFunctions
    {
        private readonly IFunctionEndpoint _functionEndpoint;

        public FplBotFunctions(IFunctionEndpoint functionEndpoint)
        {
            _functionEndpoint = functionEndpoint;
        }

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
            var sendOptions = new SendOptions();
            sendOptions.RouteToThisEndpoint();
            await _functionEndpoint.Send(new PublishViaWebHook("test"), sendOptions, executionContext);
            return r;
        }

        [Function("FplBotHandlers")]
        public async Task RunNServiceBusHandlers(
            [ServiceBusTrigger("%ENDPOINT_NAME%")] byte[] messageBody,
            IDictionary<string, string> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            FunctionContext context)
        {
            await _functionEndpoint.Process(messageBody, userProperties, messageId, deliveryCount, replyTo, correlationId, context);
        }
    }
}
