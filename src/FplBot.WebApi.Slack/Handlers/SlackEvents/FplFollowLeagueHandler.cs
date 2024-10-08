﻿using System.Text.RegularExpressions;
using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.WebApi.Slack.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class FplFollowLeagueHandler : HandleAppMentionBase
{
    private readonly ISlackTeamRepository _slackTeamRepository;
    private readonly ILeagueClient _leagueClient;
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ILogger<FplFollowLeagueHandler> _logger;

    public FplFollowLeagueHandler(ISlackTeamRepository slackTeamRepository, ILeagueClient leagueClient, ISlackWorkSpacePublisher publisher, ILogger<FplFollowLeagueHandler> logger)
    {
        _slackTeamRepository = slackTeamRepository;
        _leagueClient = leagueClient;
        _publisher = publisher;
        _logger = logger;
    }

    public override string[] Commands => new[] { "follow" };

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        var newLeagueId = ParseArguments(message);

        if (string.IsNullOrEmpty(newLeagueId))
        {
            var help = $"No leagueId provided. Usage: `@fplbot follow 123`";
            await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, help);
            return new EventHandledResponse(help);
        }

        int theLeagueId;

        var matches = new Regex(@"\d+").Matches(newLeagueId);
        if (matches.Select(c => c.Value).Distinct().Count() == 1)
        {
            theLeagueId = int.Parse(matches.First().Value);
        }
        else
        {
            var res = $"Could not update league to id '{newLeagueId}'. Make sure it's a single valid number.";
            await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, res);
            return new EventHandledResponse(res);
        }


        var failure = $"Could not find league {newLeagueId} :/ Could you find it at https://fantasy.premierleague.com/leagues/{newLeagueId}/standings/c ?";
        try
        {
            var league = await _leagueClient.GetClassicLeague(theLeagueId);

            if (league?.Properties != null)
            {
                var team = await _slackTeamRepository.GetTeam(eventMetadata.Team_Id);
                await _slackTeamRepository.UpdateLeagueId(eventMetadata.Team_Id, theLeagueId);
                await _slackTeamRepository.UpdateChannel(eventMetadata.Team_Id, message.Channel);
                if (!team.Subscriptions.Any())
                {
                    await _slackTeamRepository.UpdateSubscriptions(eventMetadata.Team_Id, new[] { EventSubscription.All });
                }
                var success = $"Thanks! You're now following the '{league.Properties.Name}' league (leagueId: {theLeagueId}) in {ChannelName()}";
                await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, success);
                return new EventHandledResponse(success);
                string ChannelName()
                {
                    return $"<#{message.Channel}>";
                }
            }
            await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, failure);
            return new EventHandledResponse(failure);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e.Message, e);
            await _publisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, failure);
            return new EventHandledResponse(failure);
        }

    }

    public override (string, string) GetHelpDescription() => ($"{CommandsFormatted} {{new league id}}", "Set league to follow");
}
