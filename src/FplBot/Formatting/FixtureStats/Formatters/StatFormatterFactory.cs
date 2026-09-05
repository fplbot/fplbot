using FplBot.Formatting.FixtureStats.Describers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal class StatFormatterFactory
{
    private readonly TauntData _tauntData;
    private readonly FormattingType _formattingType;

    public StatFormatterFactory(TauntData tauntData, FormattingType formattingType)
    {
        _tauntData = tauntData;
        _formattingType = formattingType;
    }

    public IFormat Create(StatType type)
    {
        return type switch
        {
            StatType.GoalsScored => CreateFormatter(new GoalDescriber()),
            StatType.Assists => CreateFormatter(new AssistDescriber()),
            StatType.OwnGoals => CreateFormatter(new OwnGoalDescriber()),
            StatType.RedCards => CreateFormatter(new RedCardDescriber()),
            StatType.PenaltiesMissed => CreateFormatter(new PenaltyMissDescriber()),
            StatType.PenaltiesSaved => CreateFormatter(new PentaltySavedDescriber()),
            _ => new NoOpFormatter()
        };
    }

    public IFormat CreateFormatter(IDescribeEvents describer)
    {
        if (_tauntData != null && describer is IDescribeTaunts tauntDescriber)
            return new TauntyFormatter(tauntDescriber, _tauntData, _formattingType);

        return new RegularFormatter(describer, _formattingType);
    }
}
