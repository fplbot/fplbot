using System;
using System.Threading.Tasks;
using Dork.Models;

namespace Dork.Abstractions
{
    public interface IHandleEvent
    {
        Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent);
        bool ShouldHandle(SlackEvent slackEvent);

        bool ShouldShowInHelp => true;
        Tuple<string, string> GetHelpDescription();
    }
}