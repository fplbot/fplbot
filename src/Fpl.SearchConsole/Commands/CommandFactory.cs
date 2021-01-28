using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Fpl.SearchConsole
{
    internal class CommandFactory
    {
        public static Command CreateIndexCommand()
        {
            Argument<string> typeArgument = new("type", "What types of documents you want to index");
            typeArgument.Suggestions.Add("entries");
            typeArgument.Suggestions.Add("leagues");
            var indexCommand = new Command("index", "index data into ElasticSearch")
            {
                typeArgument
            };
            indexCommand.Handler = CommandHandler.Create<string, IHost, CancellationToken>(CommandHandlers.IndexDataIntoElastic);
            return indexCommand;
        }

        public static Command CreateSearchCommand()
        {
            Argument<string> typeArgument = new("type", "What types of documents you want to index");
            typeArgument.Suggestions.Add("entries");
            typeArgument.Suggestions.Add("leagues");

            Argument<string> termArgument = new("term", "Search term");
            termArgument.Suggestions.Add("Magnus Carlsen");
            
            var searchCommand = new Command("search", "Search data in ElasticSearch")
            {
                typeArgument,
                termArgument
            };
            searchCommand.Handler = CommandHandler.Create<string, string, IHost>(CommandHandlers.SearchDataInElastic);
            return searchCommand;
        }
    }
}