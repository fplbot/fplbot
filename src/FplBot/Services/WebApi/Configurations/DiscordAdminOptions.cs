namespace FplBot.WebApi.Configurations;

public class DiscordAdminOptions
{
    // Comma-separated. If empty, any authenticated Discord user is treated as admin (dev default).
    public string? AllowedEmails { get; set; }
}
