namespace FplBot.Services.EventHandlers;

public class ReachStatsSweepOptions
{
    public TimeSpan DelayBetweenDiscordGuilds { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan DelayBetweenSlackChannels { get; set; } = TimeSpan.FromSeconds(1.5);
}
