using System.Text.RegularExpressions;
using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Helpers;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class FollowLeagueCommandHandler(
    ISlackTeamRepository slackTeamRepository,
    ILeagueClient leagueClient,
    ISlackWorkSpacePublisher publisher,
    ILogger<FollowLeagueCommandHandler> logger)
    : IConsumer<ProcessFollowLeagueCommand>
{
    public async Task Consume(ConsumeContext<ProcessFollowLeagueCommand> context)
    {
        var command = context.Message;
        var newLeagueId = MessageHelper.ExtractArgs(command.Text, "follow {args}");

        if (string.IsNullOrEmpty(newLeagueId))
        {
            await publisher.PublishToWorkspace(command.TeamId, command.Channel, "No leagueId provided. Usage: `@fplbot follow 123`");
            return;
        }

        int theLeagueId;

        var matches = new Regex(@"\d+").Matches(newLeagueId);
        if (matches.Select(c => c.Value).Distinct().Count() == 1)
        {
            theLeagueId = int.Parse(matches.First().Value);
        }
        else
        {
            await publisher.PublishToWorkspace(command.TeamId, command.Channel,
                $"Could not update league to id '{newLeagueId}'. Make sure it's a single valid number.");
            return;
        }

        var failure = $"Could not find league {newLeagueId} :/ Could you find it at https://fantasy.premierleague.com/leagues/{newLeagueId}/standings/c ?";
        try
        {
            var league = await leagueClient.GetClassicLeague(theLeagueId);

            if (league?.Properties != null)
            {
                var installation = await slackTeamRepository.GetInstallation(command.TeamId);
                installation.Follow(command.Channel, new ClassicLeagueId(theLeagueId));
                await slackTeamRepository.Save(installation);
                var success = $"Thanks! You're now following the '{league.Properties.Name}' league (leagueId: {theLeagueId}) in <#{command.Channel}>";
                await publisher.PublishToWorkspace(command.TeamId, command.Channel, success);
                return;
            }

            await publisher.PublishToWorkspace(command.TeamId, command.Channel, failure);
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e.Message, e);
            await publisher.PublishToWorkspace(command.TeamId, command.Channel, failure);
        }
    }
}
