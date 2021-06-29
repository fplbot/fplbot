using System.Threading.Tasks;
using FplBot.Functions.Messaging.Internal;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Functions
{
    public class AuditHandler : IHandleMessages<AppInstalled>, IHandleMessages<AppUninstalled>
    {
        public async Task Handle(AppInstalled message, IMessageHandlerContext context)
        {
            await PublishToAuditChannel(context, $"🎉 A new workspace ('{message.TeamName}') installed @fplbot!");
        }

        public async Task Handle(AppUninstalled message, IMessageHandlerContext context)
        {
            await PublishToAuditChannel(context,$"😔 '{message.TeamName}' decided to uninstall @fplbot");
        }

        private async Task PublishToAuditChannel(IMessageHandlerContext messageHandlerContext, string message)
        {
            await messageHandlerContext.SendLocal(new PublishViaWebHook(message));
        }
    }
}
