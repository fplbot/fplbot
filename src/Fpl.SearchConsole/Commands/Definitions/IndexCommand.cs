using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using Fpl.SearchConsole.Commands.Handlers;
using Microsoft.Extensions.Hosting;

namespace Fpl.SearchConsole.Commands.Definitions
{
    internal static class IndexCommand
    {
        public static Command Create()
        {
            Argument<string> typeArgument = new("type", "What types of documents you want to index");
            typeArgument.Suggestions.Add("entries");
            typeArgument.Suggestions.Add("leagues");
            var indexCommand = new Command("index", "index data into ElasticSearch") {typeArgument};
            indexCommand.Handler = CommandHandler.Create<string, IHost, CancellationToken>(IndexCommandHandler.IndexDataIntoElastic);
            return indexCommand;
        }
    }
}