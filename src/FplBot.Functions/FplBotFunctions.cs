using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
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
