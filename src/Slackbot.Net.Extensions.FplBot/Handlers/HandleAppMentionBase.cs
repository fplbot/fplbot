using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public abstract class HandleAppMentionBase : IHandleAppMentions
    {
        public abstract string Command { get; }
        public abstract Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent);
        public virtual bool ShouldHandle(AppMentionEvent slackEvent) => slackEvent.Text.TextAfterFirstSpace().StartsWith(Command, StringComparison.InvariantCultureIgnoreCase);
        protected virtual string ParseArguments(AppMentionEvent message) => MessageHelper.ExtractArgs(message.Text, $"{Command} {{args}}");
    }
}