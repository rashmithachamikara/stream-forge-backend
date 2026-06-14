namespace StreamForge.Api.IntegrationTests.Support;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiIntegrationFixture>
{
    public const string Name = "API integration tests";
}
