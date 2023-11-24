using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Functions;

// public class FunctionEndpointTrigger(IFunctionEndpoint endpoint, ILogger<FunctionEndpointTrigger> logger)
// {
//     [Function("Badabing")]
//     public async Task Run(
//         [ServiceBusTrigger("FplBot.Functions")] byte[] messageBody,
//         IDictionary<string, object> userProperties,
//         string messageId,
//         int deliveryCount,
//         string replyTo,
//         string correlationId,
//         FunctionContext context)
//     {
//         logger.LogWarning("DOING STUFF!!!!!");
//         // try
//         // {
//             await endpoint.Process(messageBody, userProperties, messageId, deliveryCount, replyTo, correlationId, context);
//         // }
//         // catch (Exception)
//         // {
//         // }
//     }
// }
