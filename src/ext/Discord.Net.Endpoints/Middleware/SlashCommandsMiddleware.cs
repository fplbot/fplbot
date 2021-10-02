using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            await context.Response.WriteAsJsonAsync(await CreateJsonResponse(slashCommand));

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
            if (hasOptions)
            {
                var array = opts.EnumerateArray();
                var chosenOption = array.First();
                _logger.LogInformation($"Selected option: {chosenOption}");
                var name = chosenOption.GetProperty("name").GetString();
                string value = chosenOption.GetProperty("value").GetRawText();
                slashCommandInput = new SlashCommandInput(name, value);
            }
            var handler = _handlers.FirstOrDefault(h => h.Name == commandName);
            if (handler != null)
            {
                SlashCommandContext slashCommandContext = new(guildId, channelId, slashCommandInput);
                var handled = await handler.Handle(slashCommandContext);
                if (handled is ChannelMessageWithSourceResponse channelMessageRes)
                {
                    var res = new
                    {
                        type = handled.Type,
                        data = new
                        {
                            content = channelMessageRes.Content
                        }
                    };
                    _logger.LogTrace($"Response:\n{res}");
                    return res;
                }
                _logger.LogTrace($"Not yet ready to handle the slash command type {slashCommandType}. Unsupported in the Discord.Net Framework");
                return new
                {
                    type = 4,
                    data = new
                    {
                        content = $"I'm not ready to handle this type ({handled.Type}) of command yet 🤷‍♂️"
                    }
                };
            }
            _logger.LogWarning("No handler registered for `{commandName}`", commandName);
            return new
            {
                type = 4,
                data = new
                {
                    content = "I'm not ready to handle this command yet 🤷‍♂️"
                }
            };;
        }
    }

    public record SlashCommandContext(string GuildId, string ChannelId, SlashCommandInput CommandInput = null);
    public record SlashCommandInput(string Name, string Value);
}
