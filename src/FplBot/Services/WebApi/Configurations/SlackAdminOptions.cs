namespace FplBot.WebApi.Configurations;

public class SlackAdminOptions
{
    public string? SlackClientId { get; set; }
    public string? SlackClientSecret { get; set; }
    // Comma-separated. If empty, any authenticated Slack user is treated as admin (dev default).
    public string? AllowedUserIds { get; set; }
    // If empty, any Slack team is accepted (dev default).
    public string? AllowedTeamId { get; set; }
}
