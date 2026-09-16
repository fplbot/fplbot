using System.Collections.Concurrent;
using FplBot.Tests.E2E.Slack.SlackSubscriptions;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.AssistantThreadsSetStatus;
using Slackbot.Net.SlackClients.Http.Models.Requests.AssistantThreadsSetSuggestedPrompts;
using Slackbot.Net.SlackClients.Http.Models.Requests.AssistantThreadsSetTitle;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostEphemeral;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatUpdate;
using Slackbot.Net.SlackClients.Http.Models.Requests.FileUpload;
using Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish;
using Slackbot.Net.SlackClients.Http.Models.Responses;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatGetPermalink;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsHistoryResponse;
using Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsList;
using Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsRepliesResponse;
using Slackbot.Net.SlackClients.Http.Models.Responses.FileUpload;
using Slackbot.Net.SlackClients.Http.Models.Responses.UserProfile;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Slackbot.Net.SlackClients.Http.Models.Responses.ViewPublish;

namespace FplBot.Tests.E2E;

public class CapturingSlackClient(SlackMessageCapture capture) : ISlackClient
{
    private readonly ConcurrentDictionary<string, string> _failing = new();
    private Func<Task<Response>> _appsUninstallOutcome = DefaultAppsUninstallOutcome;

    private static Func<Task<Response>> DefaultAppsUninstallOutcome => () => Task.FromResult(new Response { Ok = true });

    public void FailChannel(string channelId, string slackError) => _failing[channelId] = slackError;

    public void RecoverChannel(string channelId) => _failing.TryRemove(channelId, out _);

    public void SetAppsUninstallResult(Response response) => _appsUninstallOutcome = () => Task.FromResult(response);

    public void SetAppsUninstallThrows(Exception exception) => _appsUninstallOutcome = () => throw exception;

    public void Reset()
    {
        _failing.Clear();
        _appsUninstallOutcome = DefaultAppsUninstallOutcome;
    }

    public Task<ChatPostMessageResponse> ChatPostMessage(ChatPostMessageRequest postMessage)
    {
        if (_failing.TryGetValue(postMessage.Channel, out var error))
        {
            return Task.FromResult(new ChatPostMessageResponse { Ok = false, Error = error });
        }

        capture.Record(postMessage);
        return Task.FromResult(new ChatPostMessageResponse { Ok = true, ts = "ts123" });
    }

    public Task<ChatPostMessageResponse> ChatPostMessage(string channel, string text) =>
        ChatPostMessage(new ChatPostMessageRequest { Channel = channel, Text = text });

    public Task<UsersListResponse> UsersList() =>
        Task.FromResult(new UsersListResponse { Ok = true, Members = [] });

    public Task<ConversationsListResponse> ConversationsListPublicChannels(int? limit = null, string? cursor = null) =>
        Task.FromResult(new ConversationsListResponse { Ok = true, Channels = [] });

    public Task<UserProfileResponse> UserProfile(string user) =>
        Task.FromResult(new UserProfileResponse { Ok = true, Profile = new GetUserProfile() });

    public Task<Response> AppsUninstall(string clientId, string clientSecret) => _appsUninstallOutcome();

    public Task<ChatPostMessageResponse> ChatPostEphemeralMessage(ChatPostEphemeralMessageRequest postMessage) =>
        throw new NotImplementedException();

    public Task<ChatPostMessageResponse> ChatUpdate(ChatUpdateRequest postMessage) =>
        throw new NotImplementedException();

    public Task<ChatGetPermalinkResponse> ChatGetPermalink(string channel, string message_ts) =>
        throw new NotImplementedException();

    public Task<Response> ReactionsAdd(string name, string channel, string timestamp) =>
        throw new NotImplementedException();

    public Task<ConversationsListResponse> ConversationsMembers(string channel) =>
        throw new NotImplementedException();

    public Task<ConversationsRepliesResponse> ConversationsReplies(string channel, string ts, int? limit = null, string? cursor = null) =>
        throw new NotImplementedException();

    public Task<ConversationsHistoryResponse> ConversationsHistory(string channel, int? limit = null, string? cursor = null) =>
        throw new NotImplementedException();

    public Task<ConversationsOpenResponse> ConversationsOpen(string[] users) =>
        throw new NotImplementedException();

    public Task<ViewPublishResponse> ViewPublish(ViewPublishRequest view) =>
        throw new NotImplementedException();

    public Task<FileUploadResponse> FilesUpload(FileUploadRequest fileupload) =>
        throw new NotImplementedException();

    public Task<FileUploadResponse> FilesUpload(FileUploadMultiPartRequest req) =>
        throw new NotImplementedException();

    public Task<Response> AssistantThreadsSetStatus(AssistantThreadsSetStatusRequest request) =>
        throw new NotImplementedException();

    public Task<Response> AssistantThreadsSetTitle(AssistantThreadsSetTitleRequest request) =>
        throw new NotImplementedException();

    public Task<Response> AssistantThreadsSetSuggestedPrompts(AssistantThreadsSetSuggestedPromptsRequest request) =>
        throw new NotImplementedException();
}
