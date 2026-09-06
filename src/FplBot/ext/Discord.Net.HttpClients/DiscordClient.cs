using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Discord.Net.HttpClients
{
    public class DiscordClient(HttpClient client, IOptions<DiscordClientOptions> options, ILogger<DiscordClient> logger)
        : IDiscordClient
    {
        public record ChannelMessage(string id);
        public async Task ChannelMessagePost(string channelId, string text)
        {
            string serialized = JsonSerializer.Serialize((object)new
            {
                content = text
            });
            var jsonContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            logger.LogInformation(serialized);
            var res = await client.PostAsync($"api/v8/channels/{channelId}/messages",jsonContent);
            string responseBody = (await res.Content.ReadAsStringAsync());
            logger.LogInformation(responseBody);
            res.EnsureSuccessStatusCode();
        }

        public record RichEmbed(string Title, string Description, int? Color = null);

        public async Task ChannelMessagePost(string channelId, RichEmbed embed)
        {
            string serialized = JsonSerializer.Serialize((object)new
            {
                embeds =  new []
                {
                    new
                    {
                        type = "rich",
                        title = embed.Title,
                        description = embed.Description,
                        color = embed.Color ?? 3604540
                    }
                }
            });
            var jsonContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            logger.LogInformation(serialized);
            var res = await client.PostAsync($"api/v8/channels/{channelId}/messages",jsonContent);
            string responseBody = (await res.Content.ReadAsStringAsync());
            logger.LogInformation(responseBody);
            res.EnsureSuccessStatusCode();
        }

        // https://discord.com/developers/docs/interactions/application-commands#application-command-object-application-command-option-type
        public async Task ApplicationsCommandPost(string name, string description, string? guildId, params ApplicationCommandOptions[] options1)
        {
            object value = new
            {
                name = name,
                description = description,
            };
            if (options1 != null && options1.Any())
            {
                var allOptions = new List<object>();
                foreach (ApplicationCommandOptions option in options1)
                {
                    object singleOption = new {
                        type = option.Type,
                        name = option.Name,
                        description = option.Description,
                        required = option.Required
                    };

                    if (option.Choices != null && option.Choices.Any())
                    {
                        singleOption = new {
                            type = option.Type,
                            name = option.Name,
                            description = option.Description,
                            required = option.Required,
                            choices = option.Choices.Select(c => new
                            {
                                name = c.Name,
                                value = c.Value
                            }).ToArray()
                        };
                    }

                    if (option.Options != null && option.Options.Any())
                    {
                        var subOptions = new List<object>();
                        foreach (ApplicationCommandOptions subOpt in option.Options)
                        {
                            object singleSubOption = new {
                                type = subOpt.Type,
                                name = subOpt.Name,
                                description = subOpt.Description,
                                required = subOpt.Required
                            };

                            if (subOpt.Choices != null && subOpt.Choices.Any())
                            {
                                singleSubOption = new {
                                    type = subOpt.Type,
                                    name = subOpt.Name,
                                    description = subOpt.Description,
                                    required = subOpt.Required,
                                    choices = subOpt.Choices.Select(c => new
                                    {
                                        name = c.Name,
                                        value = c.Value
                                    }).ToArray()
                                };
                            }

                            logger.LogInformation(singleOption.ToString());
                            subOptions.Add(singleSubOption);
                        }
                        singleOption = new {
                            type = option.Type,
                            name = option.Name,
                            description = option.Description,
                            required = option.Required,
                            options = subOptions.ToArray()
                        };
                    }
                    allOptions.Add(singleOption);
                }

                value = new
                {
                    name = name,
                    description = description,
                    options = allOptions
                };
            }

            string serialized = JsonSerializer.Serialize(value);
            logger.LogTrace($"Sending:\n{serialized}");
            var jsonContent = new StringContent(serialized, Encoding.UTF8, "application/json");

            var requestUri = $"api/v8/applications/{options.Value.DiscordApplicationId}/commands";
            if (!string.IsNullOrEmpty(guildId))
            {
                requestUri = $"api/v8/applications/{options.Value.DiscordApplicationId}/guilds/{guildId}/commands";
            }

            var res = await client.PostAsync(requestUri,jsonContent);
            string responseBody = (await res.Content.ReadAsStringAsync());
            logger.LogTrace(responseBody);
            res.EnsureSuccessStatusCode();
        }

        public async Task ApplicationsCommandDelete(string commandId)
        {
            var res = await client.DeleteAsync(
                $"api/v8/applications/{options.Value.DiscordApplicationId}/commands/{commandId}");
            res.EnsureSuccessStatusCode();
        }

        public async Task ApplicationsCommandForGuildDelete(string guildId, string commandId)
        {
            var res = await client.DeleteAsync(
                $"api/v8/applications/{options.Value.DiscordApplicationId}/guilds/{guildId}/commands/{commandId}");
            res.EnsureSuccessStatusCode();
        }

        public record ApplicationsCommand(string Id);

        public async Task<IEnumerable<ApplicationsCommand>> ApplicationsCommandForGuildGet(string guildId)
        {
            var res = await client.GetFromJsonAsync<IEnumerable<ApplicationsCommand>>(
                $"api/v8/applications/{options.Value.DiscordApplicationId}/guilds/{guildId}/commands",
                SerializerOptions);
            return res ?? Enumerable.Empty<ApplicationsCommand>();
        }

        public record Channel(long Id, string Name, int Type);


        public async Task<IEnumerable<Channel>> GuildChannelsGet(string guildId)
        {
            var res = await client.GetAsync($"/api/v8/guilds/{guildId}/channels");
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<IEnumerable<Channel>>(SerializerOptions) ?? Enumerable.Empty<Channel>();
        }

        public async Task<Guild> GuildGet(string guildId)
        {
            var res = await client.GetAsync($"/api/v8/guilds/{guildId}");
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<Guild>(SerializerOptions) ?? throw new InvalidOperationException("Failed to deserialize Guild response");
        }

        private readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
                                                                   {
                                                                       PropertyNamingPolicy = new Lowercase(),
                                                                   };
    }

    public record Guild(string Id);

    public class ApplicationCommandOptions
    {
        /// SUBCOMMAND 1
        /// SUBCOMMAND GROUP 2
        /// STRING	3
        /// INTEGER	4
        public int Type { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool Required { get; set; }

        public ApplicationCommandChoices[]? Choices { get; set; }

        public ApplicationCommandOptions[]? Options { get; set; }
    }

    public class ApplicationCommandChoices
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    internal class Lowercase : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLower();
        }
    }
}
