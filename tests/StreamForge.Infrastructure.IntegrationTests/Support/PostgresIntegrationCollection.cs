namespace StreamForge.Infrastructure.Tests.Support;

[CollectionDefinition(Name)]
public sealed class PostgresIntegrationCollection : ICollectionFixture<PostgresIntegrationFixture>
{
    public const string Name = "PostgreSQL integration tests";
}
