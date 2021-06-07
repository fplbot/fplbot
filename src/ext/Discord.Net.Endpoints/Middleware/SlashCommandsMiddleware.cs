using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Discord.Net.Endpoints.Middleware
{
    internal class SlashCommandsMiddleware
    {
        private readonly IEnumerable<ISlashCommandHandler> _handlers;
        private readonly ILogger<SlashCommandsMiddleware> _logger;

        public SlashCommandsMiddleware(RequestDelegate next, IEnumerable<ISlashCommandHandler> handlers, ILogger<SlashCommandsMiddleware> logger)
        {
            _handlers = handlers;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var slashCommand = context.Items[HttpItemKeys.SlashCommandsKey] as JsonDocument;
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync<object>(await CreateJsonResponse(slashCommand), SerializerOptions);
        }

        private async Task<object> CreateJsonResponse(JsonDocument doc)
        {
            JsonElement docRootElement = doc.RootElement;
            var data = docRootElement.GetProperty("data");
            var channelId = docRootElement.GetProperty("channel_id").GetString();
            var guildId = docRootElement.GetProperty("guild_id").GetString();
            var commandName = data.GetProperty("name").GetString();
            _logger.LogInformation($"Handling slash command {commandName}");
            var slashCommandType = data.GetProperty("type").GetInt32();
            var hasOptions = data.TryGetProperty("options", out JsonElement opts);
            SlashCommandInput slashCommandInput = null;
            var isSubCommand = false;
            if (hasOptions)
            {
                var array = opts.EnumerateArray();
                var chosenOption = array.First();
                var subCommandName = chosenOption.GetProperty("name").GetString();
                isSubCommand = chosenOption.TryGetProperty("options", out JsonElement innerOpts);
                if (isSubCommand)
                {
                    chosenOption = innerOpts.EnumerateArray().First(); // only supports single choice for simplicity
                }

                string pre = isSubCommand?"subcommand":"";
                _logger.LogInformation($"Selected {pre}option: {chosenOption}");

                JsonElement valueElement = chosenOption.GetProperty("value");
                JsonValueKind jsonValueKind = valueElement.ValueKind;
                string value = jsonValueKind == JsonValueKind.Number ? valueElement.GetInt32().ToString() : valueElement.GetString();
                slashCommandInput = new SlashCommandInput(subCommandName, value, subCommandName);
            }

            ISlashCommandHandler handler = null;
            if (isSubCommand)
            {
                handler = _handlers.FirstOrDefault(h => h.CommandName == commandName && h.SubCommandName == slashCommandInput.SubCommandName);
            }
            else
            {
                handler = _handlers.FirstOrDefault(h => h.CommandName == commandName);
            }

            if (handler != null)
            {
                SlashCommandContext slashCommandContext = new(guildId, channelId, slashCommandInput);
                var handled = await handler.Handle(slashCommandContext);
                if (handled is ChannelMessageWithSourceResponse channelMessageRes)
                {
                    _logger.LogTrace($"Response:\n{channelMessageRes}");
                    return new { type = 4, data = channelMessageRes };
                }
                if (handled is ChannelMessageWithSourceEmbedResponse channelMessageEmbedRes)
                {
                    _logger.LogTrace($"Response:\n\n{JsonSerializer.Serialize(channelMessageEmbedRes,SerializerOptions)}\n\n");
                    return new { type = 4, data = channelMessageEmbedRes };
                }
                _logger.LogTrace($"Not yet ready to handle the slash command type {slashCommandType}. Unsupported in the Discord.Net Framework");
                return new {
                    type = 4,
                    data = new
                    {
                        content = $"I'm not ready to handle this type ({handled.Type}) of command yet 🤷‍♂️"
                    }
                };
            }
            _logger.LogWarning("No handler registered for `{commandName}`", commandName);
            return new {
                type = 4,
                data = new
                {
                    content = "I'm not ready to handle this command yet 🤷‍♂️"
                }
            };
        }

        private JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = new Lowercase(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public record SlashCommandContext(string GuildId, string ChannelId, SlashCommandInput CommandInput = null);
    public record SlashCommandInput(string Name, string Value, string SubCommandName);

    internal class Lowercase : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLower();
        }
    }
}
