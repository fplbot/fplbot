using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models.Interactive.BlockActions;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface IHandleInteractiveBlockActions
    {
        Task<EventHandledResponse> Handle(BlockActionInteraction raw);
    }
}