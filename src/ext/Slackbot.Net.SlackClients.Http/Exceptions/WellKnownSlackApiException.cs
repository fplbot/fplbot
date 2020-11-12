using System;

namespace Slackbot.Net.SlackClients.Http.Exceptions
{
    public class WellKnownSlackApiException : Exception
    {
        public WellKnownSlackApiException(string error, string responseContent) : base(error)
        {
            Error = error;
            ResponseContent = responseContent;
        }

        public string ResponseContent { get; }

        public string Error { get; }
    }
}