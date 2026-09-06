using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.WebApi.Pages.Admin.TeamDetails;

public class PublishEvent : PageModel
{
    private readonly ISlackTeamRepository _teamRepo;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private IGlobalSettingsClient _gameweekClient;

    public PublishEvent(ISlackTeamRepository teamRepo, ITokenStore tokenStore, ISendEndpointProvider sendEndpointProvider, IGlobalSettingsClient gameweekClient)
    {
        _teamRepo = teamRepo;
        _sendEndpointProvider = sendEndpointProvider;
        _gameweekClient = gameweekClient;
    }

    public async Task OnGet(string teamId)
    {
        var teamIdToUpper = teamId.ToUpper();
        Team = await _teamRepo.GetTeam(teamIdToUpper);
    }

    public async Task<IActionResult> OnPost(string teamId, EventSubscription[] subscriptions)
    {
        if (subscriptions == null || !subscriptions.Any())
        {
            TempData["msg"] += $"No subs selected..";
            return RedirectToPage(nameof(PublishEvent));
        }

        var teamIdToUpper = teamId.ToUpper();
        var team = await _teamRepo.GetTeam(teamIdToUpper);

        if (subscriptions.Contains(EventSubscription.Standings))
        {
            var settings = await _gameweekClient.GetGlobalSettings();
            var gameweek = settings!.Gameweeks.GetCurrentGameweek();
            if (team.FplbotLeagueId.HasValue && !string.IsNullOrEmpty(team.FplBotSlackChannel))
            {
                var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:FplBot.EventHandlers.Slack"));
                await endpoint.Send(new PublishStandingsToSlackWorkspace(team.TeamId ?? "", team.FplBotSlackChannel ?? "", team.FplbotLeagueId.Value, gameweek!.Id));
                TempData["msg"] = $"Published standings to {teamId}";
            }
            else
            {
                TempData["msg"] = $"Did not publish. Missing fpl league id for {teamId}";
            }
        }
        else
        {
            TempData["msg"] += $"Unsupported event. Nothing published.";
        }

        return RedirectToPage(nameof(PublishEvent));
    }

    public SlackTeam Team { get; set; } = null!;
}
