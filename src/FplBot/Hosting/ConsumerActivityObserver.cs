using System.Diagnostics;
using MassTransit;

namespace FplBot.Hosting;

public class ConsumerActivityObserver(IReadOnlyDictionary<Type, FplBotService> owners) : IConsumerConfigurationObserver
{
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator) where TConsumer : class
    {
        if (owners.TryGetValue(typeof(TConsumer), out var owner))
            configurator.UseFilter(new ConsumerActivityFilter<TConsumer>(owner));
    }

    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        var teamId = StringProperty<TMessage>(nameof(TeamContextFilter<TConsumer, TMessage>.TeamId));
        if (teamId is null)
            return;

        configurator.UseFilter(new TeamContextFilter<TConsumer, TMessage>(teamId, StringProperty<TMessage>("ChannelId")));
    }

    private static Func<TMessage, string?>? StringProperty<TMessage>(string name)
    {
        var property = typeof(TMessage).GetProperty(name);
        return property?.PropertyType == typeof(string)
            ? message => property.GetValue(message) as string
            : null;
    }
}

public class ConsumerActivityFilter<TConsumer>(FplBotService owner) : IFilter<ConsumerConsumeContext<TConsumer>>
    where TConsumer : class
{
    public async Task Send(ConsumerConsumeContext<TConsumer> context, IPipe<ConsumerConsumeContext<TConsumer>> next)
    {
        using var activity = FplBotDiagnostics.For(owner).StartActivity(typeof(TConsumer).Name);
        await next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope(nameof(ConsumerActivityFilter<TConsumer>));
}

public class TeamContextFilter<TConsumer, TMessage>(Func<TMessage, string?> teamId, Func<TMessage, string?>? channelId)
    : IFilter<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    public const string TeamId = nameof(TeamId);

    public async Task Send(ConsumerConsumeContext<TConsumer, TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
    {
        var team = teamId(context.Message);
        var channel = channelId?.Invoke(context.Message);

        Activity.Current?.SetTag(FplBotDiagnostics.TeamIdTag, team);
        if (channel is not null)
            Activity.Current?.SetTag(FplBotDiagnostics.ChannelIdTag, channel);

        var scopeValues = new Dictionary<string, object> { [FplBotDiagnostics.TeamIdTag] = team ?? string.Empty };
        if (channel is not null)
            scopeValues[FplBotDiagnostics.ChannelIdTag] = channel;

        using var scope = context.GetPayload<IServiceProvider>()
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(TConsumer))
            .BeginScope(scopeValues);

        await next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope(nameof(TeamContextFilter<TConsumer, TMessage>));
}
