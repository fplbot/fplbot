using Nest;
using System;
using System.Threading.Tasks;

namespace Fpl.Search
{
    public class ElasticClientBase : IElasticClientBase
    {
        protected readonly IElasticClient Client;

        public ElasticClientBase(SearchOptions options)
        {
            var connSettings = new ConnectionSettings(new Uri(options.IndexUri));
            Client = new ElasticClient(connSettings);
        }

        public async Task<bool> IsActiveIndex(string index)
        {
            return (await Client.Indices.ExistsAsync(index)).Exists;
        }

        public async Task<bool> DisposeIndex(string index)
        {
            return (await Client.Indices.DeleteAsync(index)).Acknowledged;
        }
    }

    public interface IElasticClientBase
    {
        Task<bool> IsActiveIndex(string index);
        Task<bool> DisposeIndex(string index);
    }
}