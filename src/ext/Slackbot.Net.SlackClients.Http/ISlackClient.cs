using System.Threading.Tasks;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish;
using Slackbot.Net.SlackClients.Http.Models.Responses;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatGetPermalink;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsList;
using Slackbot.Net.SlackClients.Http.Models.Responses.UserProfile;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Slackbot.Net.SlackClients.Http.Models.Responses.ViewPublish;

namespace Slackbot.Net.SlackClients.Http
{
    /// <summary>
    /// A slack client for endpoints that require a bot token (no human credentials)
    /// </summary>
    public interface ISlackClient
    {
        /// <summary>
        /// Scopes required: `chat:write`
        /// </summary>
        /// <remarks>https://api.slack.com/methods/chat.postMessage</remarks>
        Task<ChatPostMessageResponse> ChatPostMessage(string channel, string text);

        /// <summary>
        /// Scopes required: `chat:write`
        /// </summary>
        /// <remarks>https://api.slack.com/methods/chat.postMessage</remarks>
        Task<ChatPostMessageResponse> ChatPostMessage(ChatPostMessageRequest postMessage);

        /// <summary>
        /// Scopes required: no scopes required
        /// </summary>
        /// <remarks>https://api.slack.com/methods/chat.getPermalink</remarks>
        Task<ChatGetPermalinkResponse> ChatGetPermalink(string channel, string message_ts);

        /// <summary>
        /// Scopes required: `reactions:write`
        /// </summary>
        /// <remarks>https://api.slack.com/methods/reactions.add</remarks>
        Task<Response> ReactionsAdd(string name, string channel, string timestamp);

        /// <summary>
        /// Scopes required: `users:read`
        /// </summary>
        /// <remarks>https://api.slack.com/methods/users.list</remarks>
        Task<UsersListResponse> UsersList();

        /// <summary>
        /// Scopes required: channels:read
        /// Only requests `public_channel` types of conversations
        /// </summary>
        /// <remarks>https://api.slack.com/methods/conversations.list</remarks>
        Task<ConversationsListResponse> ConversationsListPublicChannels(int? limit = null, string cursor = null);

        /// <summary>
        /// Scopes required: channels:read | groups:read | im:read
        /// Only requests `public_channel` types of conversations
        /// </summary>
        /// <remarks>https://api.slack.com/methods/conversations.members</remarks>
        Task<ConversationsListResponse> ConversationsMembers(string channel);


        /// <summary>
        /// Scopes required: none
        /// Uninstalls an app (client_id/client_secret defines which app) in the workspace (token defines which workspace)
        /// </summary>
        /// <remarks>https://api.slack.com/methods/conversations.members</remarks>
        Task<Response> AppsUninstall(string clientId, string clientSecret);
        
        /// <summary>
        /// Scopes required: none
        /// Publish a static view for a User.
        /// </summary>
        /// <remarks>https://api.slack.com/methods/views.publish</remarks>
        Task<ViewPublishResponse> ViewPublish(ViewPublishRequest view);
        
        /// <summary>
        /// Scopes required: users.profile:read 
        /// Gets a users profile
        /// </summary>
        /// <remarks>https://api.slack.com/methods/users.profile.get</remarks>
        Task<UserProfileResponse> UserProfile(string user);
    }
}