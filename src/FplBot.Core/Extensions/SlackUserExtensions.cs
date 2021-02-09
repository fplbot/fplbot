using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace FplBot.Core.Extensions
{
    public static class SlackUserExtensions
    {
        public static bool IsActiveRealPerson(this User user)
        {
            return !user.Deleted && !user.Is_Bot;
        }
    }
}
