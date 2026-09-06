using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin.Discord;

public class Broadcast(ISendEndpointProvider sendEndpointProvider, ILogger<Admin.Broadcast> logger) : PageModel
{
    public Task OnGet() => Task.CompletedTask;

    public async Task<IActionResult> OnPost(string message, ChannelFilter selectedFilter)
    {
        logger.LogInformation($"ENQUEUEING BROADCAST TO DISCORD");
        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:FplBot.EventHandlers.Discord"));
            await endpoint.Send(new BroadcastToDiscord(message, selectedFilter));
            TempData["msg"] = $"Discord Broadcast enqueued using {selectedFilter}!";
        }
        catch (Exception e)
        {
            TempData["msg"] = $"Broadcast to Discord failed '{e}'";
        }

        return RedirectToPage("Broadcast");
    }

    public ChannelFilter ChannelFilter { get; set; }

}
