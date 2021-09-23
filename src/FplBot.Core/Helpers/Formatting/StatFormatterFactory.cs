using FplBot.Core.Abstractions;
using FplBot.Core.Taunts;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
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
            switch (type)
            {
                case StatType.GoalsScored:
                    return new GoalFormatter(_tauntData);
                case StatType.Assists:
                    return new AssistFormatter(_tauntData);
                case StatType.OwnGoals:
                    return new OwnGoalFormatter(_tauntData);
                case StatType.RedCards:
                    return new RedCardFormatter(_tauntData);
                case StatType.PenaltiesMissed:
                    return new PenaltyMissFormatter(_tauntData);
                case StatType.PenaltiesSaved:
                    return new PentaltySavedFormatter(_tauntData);
                default:
                    return new NoOpFormatter();
            }
        }


    }
}
