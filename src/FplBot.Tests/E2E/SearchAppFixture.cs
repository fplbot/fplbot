using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nest;
using Testcontainers.Elasticsearch;

namespace FplBot.Tests.E2E;

public class SearchAppFixture() : AppFixture("search-app-fixture")
{
    private readonly ElasticsearchContainer _elasticsearch =
        new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:8.17.3")
            .WithPassword("elastic")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms256m -Xmx256m")
            .WithReuse(true)
            .WithLabel("reuse-id", "search-app-fixture")
            .Build();

    public IElasticClient ElasticClient => Services.GetRequiredService<IElasticClient>();

    public override async ValueTask InitializeAsync()
    {
        await _elasticsearch.StartAsync();
        await PurgeLeftoverIndices();
        await base.InitializeAsync();
    }

    private async Task PurgeLeftoverIndices()
    {
        await new ElasticClient(ConnectionSettings())
            .Indices.DeleteAsync("entries-*,leagues-*,analytics-*");
    }

    private ConnectionSettings ConnectionSettings() =>
        new ConnectionSettings(new Uri(_elasticsearch.GetConnectionString()))
            .BasicAuthentication("elastic", "elastic")
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

    protected override void ConfigureSearchClient(IServiceCollection services)
    {
        services.RemoveAll<IElasticClient>();
        services.AddSingleton<IElasticClient>(new ElasticClient(ConnectionSettings()));
    }
}

[CollectionDefinition("AppSearch")]
public class AppSearchCollection : ICollectionFixture<SearchAppFixture>;
