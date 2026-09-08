using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin;

public class Broadcast(ISlackTeamRepository teamRepo, ISendEndpointProvider sendEndpointProvider, ILogger<Broadcast> logger)
    : PageModel
{
    public async Task OnGet()
    {
        var teams = await teamRepo.GetAllTeams();
        foreach (var t in teams)
        {
            Workspaces.Add(t);
        }
    }

    public async Task<IActionResult> OnPost(string message)
    {
        logger.LogInformation($"ENQUEUEING BROADCAST TO SLACK");
        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{nameof(EventHandlers.Slack.BroadcastToSlackHandler)}"));
            await endpoint.Send(new BroadcastToSlack(message));
            TempData["msg"] = "Slack Broadcast enqueued!";
        }
        catch (Exception e)
        {
            TempData["msg"] = $"Broadcast to Slack failed '{e}'";
        }

        return RedirectToPage("Broadcast");
    }

    public List<SlackTeam> Workspaces { get; set; } = new();
}
