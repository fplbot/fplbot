using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nest;
using Testcontainers.Elasticsearch;

namespace FplBot.Tests.E2E;

// Only the tests that actually exercise search need a real Elasticsearch, so this stays a
// separate fixture/collection from AppFixture's — every other AppFixture consumer never
// pays for a JVM it doesn't use.
public class SearchAppFixture : AppFixture
{
    private static readonly bool ReuseContainers =
        Environment.GetEnvironmentVariable("REUSE_TEST_CONTAINERS") == "true";

    // Version matches Aspire's AddElasticsearch default (see FplBot.AppHost/Program.cs) so the
    // engine tests run against is the same one local dev/prod actually use.
    private readonly ElasticsearchContainer _elasticsearch =
        new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:8.17.3")
            .WithPassword("elastic")
            .WithReuse(ReuseContainers)
            .WithLabel("reuse-id", "search-app-fixture")
            .Build();

    public override async ValueTask InitializeAsync()
    {
        await _elasticsearch.StartAsync();
        await base.InitializeAsync();
    }

    protected override void ConfigureSearchClient(IServiceCollection services)
    {
        var settings = new ConnectionSettings(new Uri(_elasticsearch.GetConnectionString()))
            .BasicAuthentication("elastic", "elastic")
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

        services.RemoveAll<IElasticClient>();
        services.AddSingleton<IElasticClient>(new ElasticClient(settings));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (!ReuseContainers) await _elasticsearch.DisposeAsync();
    }
}

[CollectionDefinition("AppSearch")]
public class AppSearchCollection : ICollectionFixture<SearchAppFixture>;
