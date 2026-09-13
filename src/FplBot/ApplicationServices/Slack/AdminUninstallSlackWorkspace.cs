using FplBot.Data.Slack;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.ApplicationServices.Slack;

// A distinct use case from UninstallSlackWorkspace, not a silent variant of it: this runs when
// an admin cleans up a workspace whose token is already dead, so there's no live uninstall to
// tell anyone about - nothing publishes here.
public class AdminUninstallSlackWorkspace(ISlackTeamRepository repository)
{
    public async Task<Workspace?> Execute(string teamId)
    {
        var team = await repository.FindByTeamId(teamId);
        if (team is null)
        {
            return null;
        }

        var installation = SlackInstallationMapper.ToDomain(team);
        installation.Uninstall();

        await repository.DeleteByTeamId(team.TeamId!);

        return new Workspace(team.TeamId!, team.TeamName, team.AccessToken ?? string.Empty);
    }
}
