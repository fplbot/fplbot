using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Models.Interactive.ViewSubmissions;

namespace Slackbot.Net.Endpoints.Abstractions
{
    public interface IHandleViewSubmissions
    {
        Task<EventHandledResponse> Handle(ViewSubmission payload);
    }
}