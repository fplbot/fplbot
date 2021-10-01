using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;

namespace FplBot.Slack.Helpers.Formatting.FixtureStats
{
    internal class StatFormatterFactory
    {
        private readonly TauntData _tauntData;

        public StatFormatterFactory(TauntData tauntData)
        {
            _tauntData = tauntData;
        }

        public IFormatEvents Create(StatType type)
        {
            return type switch
            {
                StatType.GoalsScored => new GoalFormatter(_tauntData),
                StatType.Assists => new AssistFormatter(_tauntData),
                StatType.OwnGoals => new OwnGoalFormatter(_tauntData),
                StatType.RedCards => new RedCardFormatter(_tauntData),
                StatType.PenaltiesMissed => new PenaltyMissFormatter(_tauntData),
                StatType.PenaltiesSaved => new PentaltySavedFormatter(_tauntData),
                StatType.YellowCards => new NoOpFormatter(),
                StatType.Unknown => new NoOpFormatter(),
                _ => new NoOpFormatter()
            };
        }


    }
}
