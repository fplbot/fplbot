namespace FplBot.WebApi.Configurations;

public class AdminAllowedEmails
{
    // Comma-separated. Authorizes admin login for both Slack and Discord by email.
    // Empty is permissive locally (dev default) and fails closed everywhere else.
    public string? AllowedEmails { get; set; }
}
