using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.VerifiedEntries.Extensions;
using MassTransit;

namespace FplBot.WebApi.Handlers.Sagas;

public class ThrottleSuggestionConstants
{
    public const int ThrottleTimeoutInSeconds = 60;
    public const string SlackChannel = "#fplbot-notifications";
    public const string TeamId = "T016B9N3U7P";
}

public record VerifiedEntrySuggestionReceived(int EntryId, string Description);
public record VerifiedPLEntrySuggestionReceived(int EntryId, string Description, int PlayerId);

public class SuggestionsThrottleTimeout
{
    public Guid CorrelationId { get; set; }
}

public record PublishAggregatedEntrySuggestions(int EntryId, string[] Descriptions, int SuggestionCount);
public record PublishAggregatedPLEntrySuggestions(int EntryId, string[] Descriptions, int SuggestionCount, int? PlayerId);

public class AcccumulatedSuggestionsSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public int EntryId { get; set; }
    public int SuggestionCount { get; set; }
    public List<string> Descriptions { get; set; } = new();
    public Guid? TimeoutTokenId { get; set; }
}

public class AcccumulatedPLSuggestionsSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public int EntryId { get; set; }
    public int SuggestionCount { get; set; }
    public List<string> Descriptions { get; set; } = new();
    public int PlayerId { get; set; }
    public Guid? TimeoutTokenId { get; set; }
}

public class ThrottleEntrySuggestionsSagaStateMachine : MassTransitStateMachine<AcccumulatedSuggestionsSagaState>
{
    public State Throttling { get; private set; } = null!;
    public Event<VerifiedEntrySuggestionReceived> SuggestionReceived { get; private set; } = null!;
    public Schedule<AcccumulatedSuggestionsSagaState, SuggestionsThrottleTimeout> ThrottleTimeout { get; private set; } = null!;

    public ThrottleEntrySuggestionsSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Use deterministic Guid from EntryId so all suggestions for the same entry hit the same saga instance
        Event(() => SuggestionReceived, x =>
            x.CorrelateById(ctx => GuidFromEntryId(ctx.Message.EntryId)));

        Schedule(() => ThrottleTimeout, state => state.TimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromSeconds(ThrottleSuggestionConstants.ThrottleTimeoutInSeconds);
            s.Received = r => r.CorrelateById(ctx => ctx.Message.CorrelationId);
        });

        Initially(
            When(SuggestionReceived)
                .Then(ctx =>
                {
                    ctx.Saga.EntryId = ctx.Message.EntryId;
                    ctx.Saga.SuggestionCount++;
                    if (!ctx.Saga.Descriptions.Contains(ctx.Message.Description))
                        ctx.Saga.Descriptions.Add(ctx.Message.Description);
                })
                .Schedule(ThrottleTimeout, ctx => new SuggestionsThrottleTimeout { CorrelationId = ctx.Saga.CorrelationId })
                .TransitionTo(Throttling));

        During(Throttling,
            When(SuggestionReceived)
                .Then(ctx =>
                {
                    ctx.Saga.SuggestionCount++;
                    if (!ctx.Saga.Descriptions.Contains(ctx.Message.Description))
                        ctx.Saga.Descriptions.Add(ctx.Message.Description);
                })
                .Schedule(ThrottleTimeout, ctx => new SuggestionsThrottleTimeout { CorrelationId = ctx.Saga.CorrelationId }),
            When(ThrottleTimeout.Received)
                .PublishAsync(ctx => ctx.Init<PublishAggregatedEntrySuggestions>(
                    new PublishAggregatedEntrySuggestions(ctx.Saga.EntryId, ctx.Saga.Descriptions.ToArray(), ctx.Saga.SuggestionCount)))
                .Finalize());

        SetCompletedWhenFinalized();
    }

    private static Guid GuidFromEntryId(int entryId) =>
        new(entryId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public class ThrottlePlSuggestionsSagaStateMachine : MassTransitStateMachine<AcccumulatedPLSuggestionsSagaState>
{
    public State Throttling { get; private set; } = null!;
    public Event<VerifiedPLEntrySuggestionReceived> SuggestionReceived { get; private set; } = null!;
    public Schedule<AcccumulatedPLSuggestionsSagaState, SuggestionsThrottleTimeout> ThrottleTimeout { get; private set; } = null!;

    public ThrottlePlSuggestionsSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => SuggestionReceived, x =>
            x.CorrelateById(ctx => GuidFromEntryId(ctx.Message.EntryId)));

        Schedule(() => ThrottleTimeout, state => state.TimeoutTokenId, s =>
        {
            s.Delay = TimeSpan.FromSeconds(ThrottleSuggestionConstants.ThrottleTimeoutInSeconds);
            s.Received = r => r.CorrelateById(ctx => ctx.Message.CorrelationId);
        });

        Initially(
            When(SuggestionReceived)
                .Then(ctx =>
                {
                    ctx.Saga.EntryId = ctx.Message.EntryId;
                    ctx.Saga.PlayerId = ctx.Message.PlayerId;
                    ctx.Saga.SuggestionCount++;
                    if (!ctx.Saga.Descriptions.Contains(ctx.Message.Description))
                        ctx.Saga.Descriptions.Add(ctx.Message.Description);
                })
                .Schedule(ThrottleTimeout, ctx => new SuggestionsThrottleTimeout { CorrelationId = ctx.Saga.CorrelationId })
                .TransitionTo(Throttling));

        During(Throttling,
            When(SuggestionReceived)
                .Then(ctx =>
                {
                    ctx.Saga.SuggestionCount++;
                    ctx.Saga.PlayerId = ctx.Message.PlayerId;
                    if (!ctx.Saga.Descriptions.Contains(ctx.Message.Description))
                        ctx.Saga.Descriptions.Add(ctx.Message.Description);
                })
                .Schedule(ThrottleTimeout, ctx => new SuggestionsThrottleTimeout { CorrelationId = ctx.Saga.CorrelationId }),
            When(ThrottleTimeout.Received)
                .PublishAsync(ctx => ctx.Init<PublishAggregatedPLEntrySuggestions>(
                    new PublishAggregatedPLEntrySuggestions(ctx.Saga.EntryId, ctx.Saga.Descriptions.ToArray(), ctx.Saga.SuggestionCount, ctx.Saga.PlayerId)))
                .Finalize());

        SetCompletedWhenFinalized();
    }

    private static Guid GuidFromEntryId(int entryId) =>
        new(entryId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public class AggregatedSuggestionsHandler : IConsumer<PublishAggregatedEntrySuggestions>, IConsumer<PublishAggregatedPLEntrySuggestions>
{
    private readonly IGlobalSettingsClient _settings;
    private readonly IEntryClient _entryClient;

    public AggregatedSuggestionsHandler(IGlobalSettingsClient settings, IEntryClient entryClient)
    {
        _settings = settings;
        _entryClient = entryClient;
    }

    public async Task Consume(ConsumeContext<PublishAggregatedEntrySuggestions> context)
    {
        var message = context.Message;
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
        await context.Publish(new PublishToSlack(ThrottleSuggestionConstants.TeamId, ThrottleSuggestionConstants.SlackChannel, "Verified suggestion: " + text));
    }

    public async Task Consume(ConsumeContext<PublishAggregatedPLEntrySuggestions> context)
    {
        var message = context.Message;
        string text;
        try
        {
            var entry = await _entryClient.Get(message.EntryId);
            var settings = await _settings.GetGlobalSettings();
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
        await context.Publish(new PublishToSlack(ThrottleSuggestionConstants.TeamId, ThrottleSuggestionConstants.SlackChannel, "Verified suggestion: " + text));
    }

    private string Counting(int count) => count > 1 ? $" {count} times" : string.Empty;

    private string Link(BasicEntry entry) =>
        $"<https://fantasy.premierleague.com/entry/{entry.Id}/event/{entry.CurrentEvent}|{entry.TeamName}>";
}
