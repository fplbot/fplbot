using FplBot.Tests.E2E;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplHandlersTests(AppFixture fixture)
{
    [Theory]
    [InlineData("<@BOTID123> subscribe standings", typeof(FplSubscribeCommandHandler))]
    [InlineData("<@BOTID123> subscribe captains", typeof(FplSubscribeCommandHandler))]
    [InlineData("<@BOTID123> subscribe pricechanges", typeof(FplSubscribeCommandHandler))]
    [InlineData("<@BOTID123> subscribe transfers", typeof(FplSubscribeCommandHandler))]
    [InlineData("<@BOTID123> unsubscribe transfers", typeof(FplSubscribeCommandHandler))]
    [InlineData("<@BOTID123> standings", typeof(FplStandingsCommandHandler))]
    [InlineData("<@BOTID123> transfers", typeof(FplTransfersCommandHandler))]
    [InlineData("<@BOTID123> pricechanges", typeof(FplPricesHandler))]
    [InlineData("<@BOTID123> pricechanges", typeof(FplPricesHandler))] // NB: non breaking white space
    [InlineData("<@BOTID123> unsubscribe FixtureGoals", typeof(FplSubscribeCommandHandler))] // NB: non breaking white space
    public void OnlyExpectedSupportedHandlersShouldHandleCommand(string input, Type expectedSupportedHandler)
    {
        // Arrange
        var mentionEvent = new AppMentionEvent { Text = input };
        using var scope = fixture.Services.CreateScope();
        var allHandlers = scope.ServiceProvider.GetServices<IHandleAppMentions>();

        // Act / assert
        foreach (var handler in allHandlers)
        {
            var handlerShouldHandleCommand = handler.ShouldHandle(mentionEvent);
            var handlerIsExpectedSupportedHandler = handler.GetType() == expectedSupportedHandler;
            if (handlerIsExpectedSupportedHandler)
            {
                Assert.True(handlerShouldHandleCommand, $"{handler.GetType().Name} should've handled \"{input}\", but it didn't");
            }
            else
            {
                Assert.False(handlerShouldHandleCommand, $"{handler.GetType().Name} shouldn't handle \"{input}\"");
            }
        }
    }
}
