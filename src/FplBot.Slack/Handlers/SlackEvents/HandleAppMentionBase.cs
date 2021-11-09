using FplBot.Slack.Extensions;
using FplBot.Slack.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Slack.Handlers.SlackEvents;

public abstract class HandleAppMentionBase : IHandleAppMentions
{
    public abstract string[] Commands { get; }
    public string CommandsFormatted => string.Join(",", Commands);
    public abstract Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent);
    public virtual bool ShouldHandle(AppMentionEvent slackEvent) => Commands.Any(c => slackEvent.Text.TextAfterFirstSpace().StartsWith(c, StringComparison.InvariantCultureIgnoreCase));
    protected string ParseArguments(AppMentionEvent message) => MessageHelper.ExtractArgs(message.Text, Commands.Select(c => $"{c} {{args}}").ToArray());
    public abstract (string, string) GetHelpDescription();
}