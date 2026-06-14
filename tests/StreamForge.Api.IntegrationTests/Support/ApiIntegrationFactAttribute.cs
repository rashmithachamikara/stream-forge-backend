namespace StreamForge.Api.IntegrationTests.Support;

public sealed class ApiIntegrationFactAttribute : FactAttribute
{
    public ApiIntegrationFactAttribute()
    {
        if (!ApiIntegrationFixture.IsEnabled)
        {
            Skip = "API integration test. Run with STREAMFORGE_RUN_API_INTEGRATION_TESTS=true and filter Category=ApiIntegration.";
        }
    }
}
