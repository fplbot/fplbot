using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Functions
{
    public class FplBotFunctions
    {
        readonly IFunctionEndpoint _endpoint;

        public FplBotFunctions(IFunctionEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        [FunctionName("Pong")]
        public static string Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Query["name"];

            return $"Hello, {name}";
        }

        [FunctionName("FplBotHandlers")]
        public async Task RunTest(
            [ServiceBusTrigger(queueName: "%QueueName%", Connection = "ASB_CONNECTIONSTRING")]
            Message message,
            ILogger logger,
            ExecutionContext executionContext)
        {
            await _endpoint.Process(message, executionContext, logger);
        }
    }
}
