using FplBot.Functions.Messaging.Internal;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Functions.Messaging.Events;

public class AuditHandler : IHandleMessages<AppInstalled>, IHandleMessages<AppUninstalled>, IHandleMessages<UnknownAppMentionReceived>
{
    public async Task Handle(AppInstalled message, IMessageHandlerContext context)
    {
        string definition = message.TeamId.StartsWith("T") ? "Slack workspace": "Discord guild";
        await PublishToAuditChannel(context, $"🎉 A new {definition} ('{message.TeamName}') installed @fplbot!");
    }

    public async Task Handle(AppUninstalled message, IMessageHandlerContext context)
    {
        await PublishToAuditChannel(context,$"😔 '{message.TeamName}' decided to uninstall @fplbot");
    }

    public async Task Handle(UnknownAppMentionReceived message, IMessageHandlerContext context)
    {
        await PublishToAuditChannel(context, $"Unhandled app_mention:\n * [{message.Team_Id}-{message.User}] \"{message.Text}\"");
    }

    private async Task PublishToAuditChannel(IMessageHandlerContext messageHandlerContext, string message)
    {
        await messageHandlerContext.SendLocal(new PublishViaWebHook(message));
    }


}