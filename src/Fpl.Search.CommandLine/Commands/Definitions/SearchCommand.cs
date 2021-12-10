using System.CommandLine;
using System.CommandLine.Invocation;
using Fpl.SearchConsole.Commands.Handlers;

namespace Fpl.SearchConsole.Commands.Definitions;

internal static class SearchCommand
{
    public static Command Create()
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
        searchCommand.Handler = CommandHandler.Create<string, string, IHost>(SearchHandler.SearchDataInElastic);
        return searchCommand;
    }
}
