using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NServiceBus;

namespace FplBot.WebApi.Pages.Admin.Discord;

public class Broadcast(IMessageSession session, ILogger<Admin.Broadcast> logger) : PageModel
{
    public Task OnGet() => Task.CompletedTask;

    public async Task<IActionResult> OnPost(string message, ChannelFilter selectedFilter)
    {
        logger.LogInformation($"ENQUEUEING BROADCAST TO DISCORD");
        try
        {
            var sendOptions = new SendOptions();
            sendOptions.SetDestination("FplBot.EventHandlers.Discord");
            await session.Send(new BroadcastToDiscord(message, selectedFilter), sendOptions);
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
