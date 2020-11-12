namespace Slackbot.Net.SlackClients.Http
{
    public interface ISlackClientBuilder
    {
        /// <summary>
        /// Build a SlackClient from a token
        /// </summary>
        ISlackClient Build(string token);
    }
}