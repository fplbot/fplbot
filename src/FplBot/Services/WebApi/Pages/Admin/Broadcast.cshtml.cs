using FplBot.Data.Slack;
using FplBot.WebApi.Slack.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin;

public class Broadcast(ISlackTeamRepository teamRepo, ISlackWorkSpacePublisher publisher, ILogger<Broadcast> logger)
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
        logger.LogInformation($"BROADCASTING TO ALL WORKSPACES");
        try
        {
            await publisher.PublishToAllWorkspaceChannels(message);
            TempData["msg"] = "Broadcasted!";
        }
        catch (Exception e)
        {
            TempData["msg"] = $"Broadcast failed '{e}'";
        }

        return RedirectToPage("Broadcast");
    }

    public List<SlackTeam> Workspaces { get; set; } = new();
}
