namespace FplBot.Messaging.Contracts.Events.v1;

public enum UninstallReason
{
    SelfUninstalled,
    AutoPurged,
    AdminDeleted
}

public record AppUninstalled(string TeamId, string TeamName, UninstallReason Reason);
