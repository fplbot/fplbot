
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using FplBot.Core.Helpers.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace FplBot.WebApi.Handlers.Commands
{
    public class ThrottleSuggestionConstants
    {
        public const int ThrottleTimeoutInSeconds = 60;
        public const string SlackChannel = "#fplbot-notifications";
        public const string TeamId = "T016B9N3U7P";
    }

    public record VerifiedEntrySuggestionReceived(int EntryId, string Description) : IEvent;
    public record VerifiedPLEntrySuggestionReceived(int EntryId,string Description, int PlayerId) : IEvent;

    public record SuggestionsThrottleTimeout();

    public class ThrottleEntrySuggestionsSaga : Saga<AcccumulatedSuggestionsSagaData>,
        IAmStartedByMessages<VerifiedEntrySuggestionReceived>,
        IHandleTimeouts<SuggestionsThrottleTimeout>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AcccumulatedSuggestionsSagaData> mapper)
        {
            mapper.ConfigureMapping<VerifiedEntrySuggestionReceived>(message => message.EntryId).ToSaga(sagaData => sagaData.EntryId);
        }

        public async Task Handle(VerifiedEntrySuggestionReceived message, IMessageHandlerContext context)
        {
            Data.SuggestionCount++;
            if(!Data.Descriptions.Contains(message.Description))
                Data.Descriptions.Add(message.Description);
            await RequestTimeout(context, TimeSpan.FromSeconds(ThrottleSuggestionConstants.ThrottleTimeoutInSeconds), new SuggestionsThrottleTimeout());
        }

        public async Task Timeout(SuggestionsThrottleTimeout state, IMessageHandlerContext context)
        {
            await context.SendLocal(new PublishAggregatedEntrySuggestions(Data.EntryId, Data.Descriptions.ToArray(), Data.SuggestionCount));
            MarkAsComplete();
        }
    }

    public class ThrottlePlSuggestionsSaga : Saga<AcccumulatedPLSuggestionsSagaData>,
        IAmStartedByMessages<VerifiedPLEntrySuggestionReceived>,
        IHandleTimeouts<SuggestionsThrottleTimeout>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AcccumulatedPLSuggestionsSagaData> mapper)
        {
            mapper.ConfigureMapping<VerifiedPLEntrySuggestionReceived>(message => message.EntryId).ToSaga(sagaData => sagaData.EntryId);
        }

        public async Task Handle(VerifiedPLEntrySuggestionReceived message, IMessageHandlerContext context)
        {
            Data.SuggestionCount++;
            Data.PlayerId = message.PlayerId;
            if(!Data.Descriptions.Contains(message.Description))
                Data.Descriptions.Add(message.Description);

            await RequestTimeout(context, TimeSpan.FromSeconds(ThrottleSuggestionConstants.ThrottleTimeoutInSeconds), new SuggestionsThrottleTimeout());
        }

        public async Task Timeout(SuggestionsThrottleTimeout state, IMessageHandlerContext context)
        {
            await context.SendLocal(new PublishAggregatedPLEntrySuggestions(Data.EntryId, Data.Descriptions.ToArray(), Data.SuggestionCount, Data.PlayerId));
            MarkAsComplete();
        }
    }

    public class AcccumulatedPLSuggestionsSagaData : AcccumulatedSuggestionsSagaData
    {
        public int PlayerId { get; set; }
    }

    public class AcccumulatedSuggestionsSagaData : ContainSagaData
    {
        public AcccumulatedSuggestionsSagaData()
        {
            Descriptions = new List<string>();
        }

        public int EntryId { get; set; }
        public int SuggestionCount { get; set; }

        public List<string> Descriptions { get; set; }
    }

    public record PublishAggregatedEntrySuggestions(int EntryId, string[] Descriptions, int SuggestionCount) : ICommand;
    public record PublishAggregatedPLEntrySuggestions(int EntryId, string[] Descriptions, int SuggestionCount, int? PlayerId) : ICommand;

    public class AggregatedSuggestionsHandler : IHandleMessages<PublishAggregatedEntrySuggestions>, IHandleMessages<PublishAggregatedPLEntrySuggestions>
    {
        private readonly IGlobalSettingsClient _settings;
        private readonly IEntryClient _entryClient;

        public AggregatedSuggestionsHandler(IGlobalSettingsClient settings, IEntryClient entryClient)
        {
            _settings = settings;
            _entryClient = entryClient;
        }

        public async Task Handle(PublishAggregatedEntrySuggestions message, IMessageHandlerContext context)
        {
            string text;
            try
            {
                var entry = await _entryClient.Get(message.EntryId);
                text = $"{Link(entry)} for {entry.PlayerFullName}{Counting(message.SuggestionCount)}. \n{Formatter.BulletPoints(message.Descriptions)}";
            }
            catch (Exception)
            {
                text = $"{message.EntryId} suggested{Counting(message.SuggestionCount)}, but it does not exist 🤷‍♂️. \n{Formatter.BulletPoints(message.Descriptions)}";
            }
            await context.SendLocal(new PublishToSlack(ThrottleSuggestionConstants.TeamId, ThrottleSuggestionConstants.SlackChannel, "Verified suggestion: " + text));
        }

        public async Task Handle(PublishAggregatedPLEntrySuggestions message, IMessageHandlerContext context)
        {
            string text;
            try
            {
                var entry = await _entryClient.Get(message.EntryId);
                var settings =  await _settings.GetGlobalSettings();
                var player = settings.Players.Get(message.PlayerId);
                if (player != null)
                {
                    var team = settings.Teams.Get(player.TeamId);
                    text = $"{Link(entry)} for {player.FullName} ({team.ShortName}){Counting(message.SuggestionCount)}";
                }
                else
                {
                    text = $"{Link(entry)} for unknown PL player {message.PlayerId}{Counting(message.SuggestionCount)}!";
                }
            }
            catch (Exception)
            {
                text = $"{message.EntryId} suggested{Counting(message.SuggestionCount)}, but it does not exist. 🤷‍♂️";
            }

            await context.SendLocal(new PublishToSlack(ThrottleSuggestionConstants.TeamId, ThrottleSuggestionConstants.SlackChannel, "Verified suggestion: " + text));

        }

        private string Counting(int count)
        {
            if (count > 1)
            {
                return $" {count} times";
            }

            return string.Empty;
        }

        private string Link(BasicEntry entry)
        {
            return $"<https://fantasy.premierleague.com/entry/{entry.Id}/event/{entry.CurrentEvent}|{entry.TeamName}>";
        }
    }


}
