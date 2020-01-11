using System.Threading.Tasks;
using FakeItEasy;
using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Handlers;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FplNextGwHandlerTests
    {
        private readonly IHandleMessages _client;

        public FplNextGwHandlerTests(ITestOutputHelper logger)
        {
            _client = Factory.GetHandler<FplNextGameweekCommandHandler>(logger);
        }
        
        [Theory]
        [InlineData("@fplbot nextgw")]
        public async Task GetPlayerHandler(string input)
        {
            A.CallTo(() => Factory.SlackClient.UsersList())
                .Returns(new UsersListResponse
                {
                    Ok = true, Members = new []
                    {
                        new Slackbot.Net.SlackClients.Http.Models.Responses.UsersList.User
                        {
                            Id = "123"
                        },
                    }
                });
            var playerData = await _client.Handle(new SlackMessage
            {
                Text = input,
                ChatHub = new ChatHub(),
                User = new Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived.User
                {
                    Id = "123"
                }
            });
            
            Assert.NotEmpty(playerData.HandledMessage);
        }
    }
}