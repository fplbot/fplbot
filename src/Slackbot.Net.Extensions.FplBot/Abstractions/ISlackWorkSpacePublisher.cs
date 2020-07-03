using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using System;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle
{
    public interface ISlackWorkSpacePublisher
    {
        Task PublishToAllWorkspaces(Func<User[], string> msg);
        Task PublishUsingToken(string token, params Func<User[], string>[] messages);
        Task PublishToSingleWorkspaceConnectedToLeague(int leagueId, params Func<User[], string>[] messages);
    }
}