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
/// Stands in for a real ISlackClient in dev environments so nothing ever reaches the real
/// Slack API: writes are logged and answered with a canned success, reads return static
/// fake data. This is what lets the whole admin UI (and anything else built against
/// ISlackClient) work end to end against Redis-seeded fake teams/guilds without a real
/// bot token — see DevSeederLifecycleHook's "TeamId-DEV-SLACK" seed, whose channel id
/// (C0DEV000001) is deliberately included below so admin team-edit's channel lookup
/// succeeds against it out of the box.
/// </summary>
public class DevLoggingSlackClient(ILogger<DevLoggingSlackClient> logger) : ISlackClient
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

    // Reads: static fake data, no real API call — see class remarks. The seeded dev team's
    // channel (C0DEV000001) is included in every channel-bearing response so admin flows
    // that resolve/validate a channel work against it without any real Slack credentials.
    private static readonly Conversation[] FakeChannels =
    [
        new Conversation { Id = "C0DEV000001", Name = "dev-fplbot", Is_Channel = true, Is_General = false },
        new Conversation { Id = "C0GENERAL001", Name = "general", Is_Channel = true, Is_General = true },
        new Conversation { Id = "C0RANDOM0001", Name = "random", Is_Channel = true }
    ];

    public Task<ChatGetPermalinkResponse> ChatGetPermalink(string channel, string message_ts)
    {
        logger.LogInformation("[DEV] Slack chat.getPermalink → {Channel}/{Ts} (fake)", channel, message_ts);
        return Task.FromResult(new ChatGetPermalinkResponse
        {
            Ok = true,
            Permalink = $"https://dev-fake.slack.local/archives/{channel}/p{message_ts.Replace(".", "")}"
        });
    }

    public Task<UsersListResponse> UsersList()
    {
        logger.LogInformation("[DEV] Slack users.list (fake)");
        return Task.FromResult(new UsersListResponse
        {
            Ok = true,
            Members =
            [
                new User { Id = "U0DEVUSER01", Name = "dev.user", Real_name = "Dev User", Is_Bot = false }
            ]
        });
    }

    public Task<ConversationsListResponse> ConversationsListPublicChannels(int? limit = null, string? cursor = null)
    {
        logger.LogInformation("[DEV] Slack conversations.list (fake, {Count} channels)", FakeChannels.Length);
        return Task.FromResult(new ConversationsListResponse
        {
            Ok = true,
            Channels = FakeChannels,
            Response_Metadata = new ResponseMetadata { Next_Cursor = "" }
        });
    }

    public Task<ConversationsListResponse> ConversationsMembers(string channel)
    {
        logger.LogInformation("[DEV] Slack conversations.members → {Channel} (fake)", channel);
        return Task.FromResult(new ConversationsListResponse
        {
            Ok = true,
            Channels = FakeChannels,
            Response_Metadata = new ResponseMetadata { Next_Cursor = "" }
        });
    }

    public Task<ConversationsRepliesResponse> ConversationsReplies(string channel, string ts, int? limit = null, string? cursor = null)
    {
        logger.LogInformation("[DEV] Slack conversations.replies → {Channel}/{Ts} (fake, empty)", channel, ts);
        return Task.FromResult(new ConversationsRepliesResponse { Ok = true, Messages = [] });
    }

    public Task<ConversationsHistoryResponse> ConversationsHistory(string channel, int? limit = null, string? cursor = null)
    {
        logger.LogInformation("[DEV] Slack conversations.history → {Channel} (fake, empty)", channel);
        return Task.FromResult(new ConversationsHistoryResponse
        {
            Ok = true,
            Messages = [],
            Has_More = false,
            Response_Metadata = new ResponseMetadata { Next_Cursor = "" }
        });
    }

    public Task<ConversationsOpenResponse> ConversationsOpen(string[] users)
    {
        logger.LogInformation("[DEV] Slack conversations.open → {Users} (fake)", string.Join(",", users));
        return Task.FromResult(new ConversationsOpenResponse { Ok = true, channel = new Channel { id = "D0DEVDM0001" } });
    }

    public Task<UserProfileResponse> UserProfile(string user)
    {
        logger.LogInformation("[DEV] Slack users.profile.get → {User} (fake)", user);
        return Task.FromResult(new UserProfileResponse
        {
            Ok = true,
            Profile = new GetUserProfile
            {
                Real_Name = "Dev User",
                Display_Name = "dev.user",
                Email = "dev.user@example.local"
            }
        });
    }
}
