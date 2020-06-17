using System;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface IHandleEvent
    {
        Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent);
        bool ShouldHandle(SlackEvent slackEvent);

        bool ShouldShowInHelp => true;
        Tuple<string, string> GetHelpDescription();
    }
}