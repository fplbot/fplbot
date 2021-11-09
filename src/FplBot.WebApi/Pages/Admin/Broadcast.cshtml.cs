using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FplBot.WebApi.Pages.Admin;

public class Broadcast : PageModel
{
    private readonly ISlackTeamRepository _teamRepo;
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ILogger<Broadcast> _logger;

    public Broadcast(ISlackTeamRepository teamRepo, ISlackWorkSpacePublisher publisher, ILogger<Broadcast> logger)
    {
        _teamRepo = teamRepo;
        _publisher = publisher;
        _logger = logger;
        Workspaces = new List<SlackTeam>();

    }

    public async Task OnGet()
    {
        var teams = await _teamRepo.GetAllTeams();
        foreach (var t in teams)
        {
            Workspaces.Add(t);
        }
    }

    public async Task<IActionResult> OnPost(string message)
    {
        _logger.LogInformation($"BROADCASTING TO ALL WORKSPACES");
        try
        {
            await _publisher.PublishToAllWorkspaceChannels(message);
            TempData["msg"] = "Broadcasted!";
        }
        catch (Exception e)
        {
            TempData["msg"] = $"Broadcast failed '{e}'";
        }

        return RedirectToPage("Broadcast");
    }

    public List<SlackTeam> Workspaces { get; set; }
}