using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Interactive;
using Slackbot.Net.Endpoints.Models.Interactive.BlockActions;
using Slackbot.Net.Endpoints.Models.Interactive.ViewSubmissions;

namespace Slackbot.Net.Endpoints.Middlewares
{
    internal class ViewSubmissionEvents
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ViewSubmissionEvents> _logger;
        private readonly IEnumerable<IHandleViewSubmissions> _responseHandlers;
        private readonly IEnumerable<IHandleInteractiveBlockActions> _blockActionHandlers;
        private readonly NoOpViewSubmissionHandler _noOp;

        public ViewSubmissionEvents(RequestDelegate next, ILogger<ViewSubmissionEvents> logger, IEnumerable<IHandleViewSubmissions> responseHandlers, IEnumerable<IHandleInteractiveBlockActions> blockActionHandlers, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = logger;
            _responseHandlers = responseHandlers;
            _blockActionHandlers = blockActionHandlers;
            _noOp = new NoOpViewSubmissionHandler(loggerFactory.CreateLogger<NoOpViewSubmissionHandler>());
        }

        public async Task Invoke(HttpContext context)
        {
            var payload = (Interaction) context.Items[HttpItemKeys.InteractivePayloadKey];

            switch (payload.Type)
            {
                case InteractionTypes.ViewSubmission:
                    await HandleViewSubmission(payload as ViewSubmission);
                    break;
                case InteractionTypes.BlockActions:
                    var res = await HandleBlockActions(payload as BlockActionInteraction);
                    context.Response.StatusCode = res.Response switch
                    {
                        "ERROR" => 500,
                        "VALIDATION_ERRORS" => 400,
                        _ => context.Response.StatusCode
                    };
                    await context.Response.WriteAsync(res.Response);
                    break;
                default:
                    await _noOp.Handle(payload);
                    break;
            }
        }

        private async Task<EventHandledResponse> HandleBlockActions(BlockActionInteraction payload)
        {
            var handler = _blockActionHandlers.FirstOrDefault();
            
            if (handler == null)
            {
                _logger.LogError("No handler registered for BlockAction interactions");
                return await _noOp.Handle(payload);
            }
            else
            {
                _logger.LogInformation($"Handling using {handler.GetType()}");
                try
                {
                    _logger.LogInformation($"Handling using {handler.GetType()}");
                    var response = await handler.Handle(payload);
                    _logger.LogInformation(response.Response);
                    return response;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    return new EventHandledResponse("ERROR");
                }
            }
        }

        private async Task HandleViewSubmission(ViewSubmission payload)
        {
            var handler = _responseHandlers.FirstOrDefault();
            
            if (handler == null)
            {
                _logger.LogError("No handler registered for ViewSubmission interactions");
                await _noOp.Handle(payload);
            }
            else
            {
                _logger.LogInformation($"Handling using {handler.GetType()}");
                try
                {
                    _logger.LogInformation($"Handling using {handler.GetType()}");
                    var response = await handler.Handle(payload);
                    _logger.LogInformation(response.Response);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public static bool ShouldRun(HttpContext ctx) => ctx.Items.ContainsKey(HttpItemKeys.InteractivePayloadKey);
    }
}