using System.Threading.Tasks;
using Slackbot.Net.Workers.Publishers;
using Xunit.Abstractions;

namespace FplBot.Tests.Helpers
{
    public class XUnitTestoutPublisher : IPublisher
    {
        private readonly ITestOutputHelper _logger;

        public XUnitTestoutPublisher(ITestOutputHelper logger)
        {
            _logger = logger;
        }
        public Task Publish(Notification notification)
        {
            _logger.WriteLine($"Notification sent: [To:{notification.Recipient}, msg: {notification.Msg}]");
            return Task.CompletedTask;
        }
    }
}