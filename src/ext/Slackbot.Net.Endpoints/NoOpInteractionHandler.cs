using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Interactive;

namespace Slackbot.Net.Endpoints
{
    internal class NoOpViewSubmissionHandler
    {
        private readonly ILogger<NoOpViewSubmissionHandler> _logger;

        public NoOpViewSubmissionHandler(ILogger<NoOpViewSubmissionHandler> logger)
        {
            _logger = logger;
        }
        
        public Task<EventHandledResponse> Handle(Interaction payload)
        {
            if (payload is UnknownInteractiveMessage unknown)
            {
                _logger.LogWarning($"No handler for event type `{payload.Type}`. Unsupported interactive message? Unknown raw:\n{unknown.RawJson}");
            }
            else
            {
                _logger.LogWarning($"No-op for event type `{payload.Type}`. Missing interactive handler registration?");
            }
            
            
            return Task.FromResult(new EventHandledResponse("No-op."));
        }
    }
}