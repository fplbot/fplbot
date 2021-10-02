using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Discord.Net.HttpClients
{
    public class DiscordClient
    {
        private readonly HttpClient _client;
        private readonly IOptions<DiscordClientOptions> _options;
        private readonly ILogger<DiscordClient> _logger;

        public DiscordClient(HttpClient client, IOptions<DiscordClientOptions> options, ILogger<DiscordClient> logger)
        {
            _client = client;
            _options = options;
            _logger = logger;
        }

        public record ChannelMessage(string id);
        public async Task ChannelMessagePost(string channelId, string text)
        {
            string serialized = JsonSerializer.Serialize((object)new
            {
                content = text
            });
            var jsonContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            _logger.LogInformation(serialized);
            var res = await _client.PostAsync($"api/v8/channels/{channelId}/messages",jsonContent);
            string responseBody = (await res.Content.ReadAsStringAsync());
            _logger.LogInformation(responseBody);
            res.EnsureSuccessStatusCode();
        }

        public async Task ApplicationsCommandPost(string name, string description)
        {
            var jsonContent = new StringContent(JsonSerializer.Serialize(new
            {
                name = name,
                type = 1,
                description = description
            }), Encoding.UTF8, "application/json");
            var res = await _client.PostAsync($"api/v8/applications/{_options.Value.DiscordApplicationId}/commands",jsonContent);
            res.EnsureSuccessStatusCode();
            string responseBody = (await res.Content.ReadAsStringAsync());
        }

        // https://discord.com/developers/docs/interactions/application-commands#application-command-object-application-command-option-type

        public async Task ApplicationsCommandForGuildPost(string name, string description, string guildId, ApplicationCommandOptions options = null)
        {
            object value = new
            {
                name = name,
                description = description,
            };
            if (options != null)
            {
                value = new
                {
                    name = name,
                    description = description,
                    options = new[]
                    {
                        new {
                            type = options.Type,
                            name = options.Name,
                            description = options.Description,
                            required = true
                        }
                    }
                };
            }
            var jsonContent = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
            var res = await _client.PostAsync($"api/v8/applications/{_options.Value.DiscordApplicationId}/guilds/{guildId}/commands",jsonContent);
            string responseBody = (await res.Content.ReadAsStringAsync());
            _logger.LogTrace(responseBody);
            res.EnsureSuccessStatusCode();
        }

        public async Task ApplicationsCommandDelete(long commandId)
        {
            var res = await _client.DeleteAsync(
                $"api/v8/applications/{_options.Value.DiscordApplicationId}/commands/{commandId}");
            res.EnsureSuccessStatusCode();
        }

        public record Channel(long Id, string Name, int Type);


        public async Task<IEnumerable<Channel>> GuildChannelsGet(string guildId)
        {
            var res = await _client.GetAsync($"/api/v8/guilds/{guildId}/channels");
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<IEnumerable<Channel>>(SerializerOptions);
        }

        private JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = new Lowercase(),
        };
    }

    public class ApplicationCommandOptions
    {
        /// STRING	3
        /// INTEGER	4
        public int Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
    }

    internal class Lowercase : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLower();
        }
    }
}
