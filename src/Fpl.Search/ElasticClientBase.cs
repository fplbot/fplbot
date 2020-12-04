using Nest;
using System;

namespace Fpl.Search
{
    public class ElasticClientBase 
    {
        protected readonly IElasticClient Client;

        public ElasticClientBase(SearchOptions options)
        {
            var connSettings = new ConnectionSettings(new Uri(options.IndexUri));
            Client = new ElasticClient(connSettings);
        }
    }
}