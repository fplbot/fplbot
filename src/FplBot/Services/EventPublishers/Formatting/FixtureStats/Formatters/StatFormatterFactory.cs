using FplBot.Formatting.FixtureStats.Describers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal class StatFormatterFactory(TauntData? tauntData, FormattingType formattingType)
{
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
        if (tauntData != null && describer is IDescribeTaunts tauntDescriber)
            return new TauntyFormatter(tauntDescriber, tauntData, formattingType);

        return new RegularFormatter(describer, formattingType);
    }
}
