using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Pages.Admin.TeamDetails;

public class Edit : PageModel
{
    private readonly ISlackTeamRepository _teamRepo;
    private readonly ISlackClientBuilder _builder;
    private readonly ILeagueClient _leagueClient;

    public Edit(ISlackTeamRepository teamRepo, ILeagueClient leagueClient, ISlackClientBuilder builder)
    {
        _teamRepo = teamRepo;
        _leagueClient = leagueClient;
        _builder = builder;
    }

    public async Task OnGet(string teamId)
    {
        var teamIdToUpper = teamId.ToUpper();
        var team = await _teamRepo.GetTeam(teamIdToUpper);
        Team = team;
        LeagueName = "Unknown league / league not found!";
        try
        {
            if(team.FplbotLeagueId.HasValue)
                LeagueName = (await _leagueClient.GetClassicLeague(team.FplbotLeagueId.Value)).Properties.Name;
        }
        catch (Exception)
        {
        }
    }

    public async Task<IActionResult> OnPost(string teamId, int leagueId, string channel, EventSubscription[] subscriptions)
    {
        var league = await _leagueClient.GetClassicLeague(leagueId, tolerate404:true);
        if(league == null)
        {
            TempData["msg"] = "⚠️ League does not exist.\n";
        }

        var slackClient = await CreateSlackClient(teamId);
        var channelsRes = await slackClient.ConversationsListPublicChannels(500);

        var channelsFound = channelsRes.Channels.Any(c => channel == $"#{c.Name}" || channel == c.Id);
        if (!channelsFound)
        {
            var channelsText = string.Join(',', channelsRes.Channels.Select(c => c.Name));
            TempData["msg"] += $"WARN. Could not find updated channel in via Slack API lookup. Channels: {channelsText}";
        }

        await _teamRepo.UpdateLeagueId(teamId, leagueId);
        await _teamRepo.UpdateChannel(teamId, channel);
        await _teamRepo.UpdateSubscriptions(teamId, subscriptions);

        TempData["msg"]+= "Updated!";
        return RedirectToPage("Edit");
    }

    public SlackTeam Team { get; set; }
    public string LeagueName { get; set; }

    private async Task<ISlackClient> CreateSlackClient(string teamId)
    {
        var token = await _teamRepo.GetTeam(teamId);
        var slackClient = _builder.Build(token: token.AccessToken);
        return slackClient;
    }
}
