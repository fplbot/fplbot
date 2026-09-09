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

namespace Slackbot.Net.SlackClients.Http;

/// <summary>
/// Wraps a real ISlackClient in dev environments so anything that writes to Slack
/// (posting/updating/uploading/reacting) is logged instead of sent, while reads
/// still hit the real API. Reads are harmless (no scopes to post/mutate) and are
/// often useful for local testing (e.g. resolving channel/user info).
/// </summary>
public class DevLoggingSlackClient(ISlackClient inner, ILogger<DevLoggingSlackClient> logger) : ISlackClient
{
    public Task<ChatPostMessageResponse> ChatPostMessage(string channel, string text)
    {
        logger.LogInformation("[DEV] Slack → {Channel}\n\n{Text}\n", channel, text);
        return Task.FromResult(new ChatPostMessageResponse { Ok = true, channel = channel, ts = "dev-fake-ts" });
    }

    public Task<ChatPostMessageResponse> ChatPostMessage(ChatPostMessageRequest postMessage)
    {
        logger.LogInformation("[DEV] Slack → {Channel}\n\n{Text}\n", postMessage.Channel, postMessage.Text);
        return Task.FromResult(new ChatPostMessageResponse { Ok = true, channel = postMessage.Channel, ts = "dev-fake-ts" });
    }

    public Task<ChatPostMessageResponse> ChatPostEphemeralMessage(ChatPostEphemeralMessageRequest postMessage)
    {
        logger.LogInformation("[DEV] Slack ephemeral → {Channel}/{User}\n\n{Text}\n", postMessage.Channel, postMessage.User, postMessage.Text);
        return Task.FromResult(new ChatPostMessageResponse { Ok = true, channel = postMessage.Channel, ts = "dev-fake-ts" });
    }

    public Task<ChatPostMessageResponse> ChatUpdate(ChatUpdateRequest postMessage)
    {
        logger.LogInformation("[DEV] Slack update → {Channel}/{Ts}\n\n{Text}\n", postMessage.channel, postMessage.ts, postMessage.text);
        return Task.FromResult(new ChatPostMessageResponse { Ok = true, channel = postMessage.channel, ts = postMessage.ts });
    }

    public Task<Response> ReactionsAdd(string name, string channel, string timestamp)
    {
        logger.LogInformation("[DEV] Slack reaction :{Name}: → {Channel}/{Timestamp}", name, channel, timestamp);
        return Task.FromResult(new Response { Ok = true });
    }

    public Task<Response> AppsUninstall(string clientId, string clientSecret)
    {
        logger.LogInformation("[DEV] Slack apps.uninstall (not calling real API)");
        return Task.FromResult(new Response { Ok = true });
    }

    public Task<ViewPublishResponse> ViewPublish(ViewPublishRequest view)
    {
        logger.LogInformation("[DEV] Slack view.publish → user:{UserId}", view.User_Id);
        return Task.FromResult(new ViewPublishResponse { Ok = true });
    }

    public Task<FileUploadResponse> FilesUpload(FileUploadRequest fileupload)
    {
        logger.LogInformation("[DEV] Slack file upload → {Channels}/{Title}", fileupload.Channels, fileupload.Title);
        return Task.FromResult(new FileUploadResponse { Ok = true });
    }

    public Task<FileUploadResponse> FilesUpload(FileUploadMultiPartRequest req)
    {
        logger.LogInformation("[DEV] Slack file upload → {Channels}/{Title}", req.Channels, req.Title);
        return Task.FromResult(new FileUploadResponse { Ok = true });
    }

    public Task<Response> AssistantThreadsSetStatus(AssistantThreadsSetStatusRequest request)
    {
        logger.LogInformation("[DEV] Slack assistant.threads.setStatus (not calling real API)");
        return Task.FromResult(new Response { Ok = true });
    }

    public Task<Response> AssistantThreadsSetTitle(AssistantThreadsSetTitleRequest request)
    {
        logger.LogInformation("[DEV] Slack assistant.threads.setTitle (not calling real API)");
        return Task.FromResult(new Response { Ok = true });
    }

    public Task<Response> AssistantThreadsSetSuggestedPrompts(AssistantThreadsSetSuggestedPromptsRequest request)
    {
        logger.LogInformation("[DEV] Slack assistant.threads.setSuggestedPrompts (not calling real API)");
        return Task.FromResult(new Response { Ok = true });
    }

    // Reads: pass through to the real client.
    public Task<ChatGetPermalinkResponse> ChatGetPermalink(string channel, string message_ts) => inner.ChatGetPermalink(channel, message_ts);
    public Task<UsersListResponse> UsersList() => inner.UsersList();
    public Task<ConversationsListResponse> ConversationsListPublicChannels(int? limit = null, string? cursor = null) => inner.ConversationsListPublicChannels(limit, cursor);
    public Task<ConversationsListResponse> ConversationsMembers(string channel) => inner.ConversationsMembers(channel);
    public Task<ConversationsRepliesResponse> ConversationsReplies(string channel, string ts, int? limit = null, string? cursor = null) => inner.ConversationsReplies(channel, ts, limit, cursor);
    public Task<ConversationsHistoryResponse> ConversationsHistory(string channel, int? limit = null, string? cursor = null) => inner.ConversationsHistory(channel, limit, cursor);
    public Task<ConversationsOpenResponse> ConversationsOpen(string[] users) => inner.ConversationsOpen(users);
    public Task<UserProfileResponse> UserProfile(string user) => inner.UserProfile(user);
}
