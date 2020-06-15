using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FplBot.WebApi.EventApi
{
    public interface IHandleAllEvents
    {
        Task Handle(EventMetaData eventMetadata, JObject eventJson);
    }
}