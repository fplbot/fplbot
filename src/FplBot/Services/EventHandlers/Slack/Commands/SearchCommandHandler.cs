using System.Text;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Helpers;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class SearchCommandHandler(
    ISearchService searchService,
    IGlobalSettingsClient globalSettingsClient,
    ISlackWorkSpacePublisher workSpacePublisher,
    ISlackTeamRepository slackTeamRepo,
    ILeagueClient leagueClient,
    IEntryClient entryClient,
    ILogger<SearchCommandHandler> logger)
    : IConsumer<ProcessSearchCommand>
{
    public async Task Consume(ConsumeContext<ProcessSearchCommand> context)
    {
        var command = context.Message;
        var term = MessageHelper.ExtractArgs(command.Text, "search {args}");

        Installation? installation = null;
        try
        {
            installation = await slackTeamRepo.GetInstallation(command.TeamId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to get team {teamId} during search.", command.TeamId);
        }

        var leagueId = installation?.GetChannel(command.ChannelId)?.FollowedLeagueId?.Value;

        var countryToBoost = await GetCountryToBoost(leagueId);

        var searchMetaData = GetSearchMetaData(installation, leagueId, command.User);

        var entriesTask = searchService.SearchForEntry(term ?? "", 0, 10, searchMetaData);
        var leaguesTask = searchService.SearchForLeague(term ?? "", 0, 10, searchMetaData, countryToBoost);

        var entries = await entriesTask;
        var leagues = await leaguesTask;

        var sb = new StringBuilder();
        sb.Append("Matching teams:\n");

        int? currentGameweek = null;
        if (entries.Any() || leagues.Any())
        {
            try
            {
                var globalSettings = await globalSettingsClient.GetGlobalSettings();
                var gameweeks = globalSettings!.Gameweeks;
                currentGameweek = gameweeks.GetCurrentGameweek()?.Id;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to obtain current gameweek when creating search result links");
            }
        }

        if (entries.Any())
        {
            sb.Append(Formatter.BulletPoints(entries.ExposedHits.Select(e => Formatter.FormatEntryItem(e, currentGameweek))));
            if (entries.HitCountExceedingExposedOnes > 0)
            {
                sb.Append($"\n...and {entries.HitCountExceedingExposedOnes} more");
            }
        }
        else
        {
            sb.Append("Found no matching teams :shrug:");
        }

        sb.Append("\n\nMatching leagues:\n");

        if (leagues.Any())
        {
            sb.Append(Formatter.BulletPoints(leagues.ExposedHits.Select(e => Formatter.FormatLeagueItem(e, currentGameweek))));
            if (leagues.HitCountExceedingExposedOnes > 0)
            {
                sb.Append($"\n...and {leagues.HitCountExceedingExposedOnes} more");
            }
        }
        else
        {
            sb.Append("Found no matching leagues :shrug:");
        }

        await workSpacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, sb.ToString());
    }

    private static SearchMetaData GetSearchMetaData(Installation? installation, long? leagueId, string user)
    {
        return new SearchMetaData { Team = installation?.ExternalId, FollowingFplLeagueId = leagueId?.ToString(), Actor = user, Client = QueryClient.Slack };
    }

    private async Task<string?> GetCountryToBoost(long? leagueId)
    {
        string? countryToBoost = null;
        if (leagueId != null)
        {
            var league = await leagueClient.GetClassicLeague((int)leagueId.Value);
            var adminEntry = league?.Properties?.AdminEntry;

            if (adminEntry != null)
            {
                var admin = await entryClient.Get(adminEntry.Value);
                if (admin != null)
                {
                    countryToBoost = admin.PlayerRegionShortIso;
                }
            }
        }

        return countryToBoost;
    }
}
