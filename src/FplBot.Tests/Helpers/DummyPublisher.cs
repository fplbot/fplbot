using System.Threading.Tasks;
using Slackbot.Net.Workers.Publishers;

namespace FplBot.Tests.Helpers
{
    public class DummyPublisher : IPublisher
    {
        public DummyPublisher()
        {
        }
        public Task Publish(Notification notification)
        {
            return Task.CompletedTask;
        }
    }
}