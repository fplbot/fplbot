using Elasticsearch.Net;
using Nest;
using Testcontainers.Elasticsearch;

namespace FplBot.Tests.E2E;

// One real Elasticsearch container, shared across every test class in the "Elasticsearch" collection
// (mirrors Factory's shared Redis container pattern). Each test seeds its own uniquely-named
// index/indices so tests never collide with each other on the shared instance.
public class ElasticsearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container =
        new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:8.15.0")
            .WithPassword("elastic")
            .Build();

    public IElasticClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var settings = new ConnectionSettings(new Uri(_container.GetConnectionString()))
            .BasicAuthentication("elastic", "elastic")
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

        Client = new ElasticClient(settings);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("Elasticsearch")]
public class ElasticsearchCollection : ICollectionFixture<ElasticsearchFixture>;
