using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.NServiceBusHandlers.Commands
{
    public class DebugCommandHandler : IHandleMessages<ReplyDebugInfo>
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILogger<DebugCommandHandler> _logger;
        private readonly IHttpClientFactory _factory;

        public DebugCommandHandler(ISlackTeamRepository teamRepo, ISlackWorkSpacePublisher publisher, ILogger<DebugCommandHandler> logger, IHttpClientFactory factory)
        {
            _teamRepo = teamRepo;
            _publisher = publisher;
            _logger = logger;
            _factory = factory;
        }
        
        public async Task Handle(ReplyDebugInfo message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Debugging!");
            string debugInfo = await DebugInfo();
            await _publisher.PublishToWorkspace(message.TeamId, message.Channel, debugInfo);
        }

        private async Task<string> DebugInfo()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            Version version = entryAssembly?.GetName()?.Version;
            string majorMinorPatch = $"{version?.Major}.{version?.Minor}.{version?.Build}";
            string informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var sha = informationalVersion?.Split(".").Last();

            var debugInfo = $"▪️ v{majorMinorPatch}\n" +
                            $"▪️ {informationalVersion}\n";

            var releaseNotes = await GetReleaseNotes(majorMinorPatch);
            if (!string.IsNullOrEmpty(releaseNotes))
            {
                debugInfo += releaseNotes;
            }
            else if(sha != "0")
            {
                debugInfo += $"️▪️ <https://github.com/fplbot/fplbot/tree/{sha}|{sha?.Substring(0,sha.Length-1)}>\n";
            }
            return debugInfo;
        }

        private async Task<string> GetReleaseNotes(string majorMinorPatch)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"fplbot",$"{majorMinorPatch}"));
            try
            {
                string? requestUri = $"https://api.github.com/repos/fplbot/fplbot/releases/tags/{majorMinorPatch}";
                
                var res = await httpClient.GetFromJsonAsync<Release>(requestUri);
                string resBody = res?.Body;
                var splitted = resBody?.Split("\n");
                var listed = splitted?.Select(s =>
                {
                    if (s.Contains("Merge branch"))
                        return null;
                    var shaRemoved = string.Join(" ",s.Split(" ")[1..]);
                    var replaced = Regex.Replace(shaRemoved, "#(\\d+)", replacement: $"<https://github.com/fplbot/fplbot/pull/$1|$1>");
                    return $"{GetEmoji(replaced)} {replaced}";
                }).Where(s => s != null);
                var joined = $"      {string.Join("\n      ", listed ?? new List<string>())}";
                var releaseLinks = $"▪️ <https://github.com/fplbot/fplbot/releases/tag/{majorMinorPatch}|Release notes for {majorMinorPatch}>\n" 
                                   + joined;
                return releaseLinks;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, ex.Message);
            }

            return string.Empty;
        }

        private string GetEmoji(string text)
        {
            var emoji = "";
            if (Regex.IsMatch(text, "^[Bb]ug(fix(es)?)?|^[Hh]otfix(es)?"))
                emoji += "😡";
            if(Regex.IsMatch(text, "[Rr]efactor"))
                emoji += "🔧";
            if(Regex.IsMatch(text, "[Pp]ull/"))
                emoji += "➕";
            
            if(emoji.Length < 2)
                emoji += GetRandomEmoji();
            return emoji;
        }

        private readonly IEnumerable<string> _randomPool = new List<string>
        {
            "🤠","😻", "🙇‍♀️", "👑","💄", "🎉", "✨", "🎩", "♥️", "💥", "🧨", "⚽️", "🚨", "📣", "🥑", "🏂", "🥁", "🎯", "🎳", "🎲", "🎰", "🐢", "💧", "🌈", "🎖"
            
        };
        private string GetRandomEmoji()
        {
            return _randomPool.GetRandom();
        }
    }

    internal record Release(string Body);
}