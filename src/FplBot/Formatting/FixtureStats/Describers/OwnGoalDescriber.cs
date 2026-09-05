using FplBot.Formatting.FixtureStats.Formatters;

namespace FplBot.Formatting.FixtureStats.Describers;

public class OwnGoalDescriber : IDescribeTaunts
{
    public TauntType Type => TauntType.HasPlayerInTeam;

    public string[] JokePool => new[]
    {
        "Isn't that guy in your team, {0}?",
        "That's -2pts, {0} 😬",
        "Are you playing anti-fpl, {0}?"
    };

    public string EventDescriptionSingular => "{0} scored a goal! In his own goal! {1}";
    public string EventDescriptionPlural => "{0} scored {1} own goals! {2}";
    public string EventEmoji => "🤦‍♂️";
}
