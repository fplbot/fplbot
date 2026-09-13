using FplBot.Data.Slack;
using FplBot.Domain;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Services.WebApi.Slack.Handlers.Reactors;

public class TokenManager : ITokenStore
{
    private readonly ISlackTeamRepository _repository;

    public TokenManager(ISlackTeamRepository repository)
    {
        _repository = repository;
    }

    public async Task Insert(Workspace workspace)
    {
        var installation = SlackInstallation.Install(workspace.TeamId, workspace.Token);
        await _repository.Save(SlackInstallationMapper.ToStorage(installation, workspace.TeamName));
    }

    public async Task Insert(SlackTeam slackTeam)
    {
        await _repository.Save(slackTeam);
    }

    public async Task<Workspace?> Delete(string teamId)
    {
        var team = await _repository.FindByTeamId(teamId);
        if (team is null)
        {
            return null;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.Uninstall();

        await _repository.DeleteByTeamId(team.TeamId!);
        return new Workspace(TeamId: team.TeamId!, TeamName: team.TeamName, Token: team.AccessToken ?? string.Empty);
    }
}
