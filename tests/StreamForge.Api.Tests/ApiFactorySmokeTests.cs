using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StreamForge.Api.Tests;

public sealed class ApiFactorySmokeTests
{
    [Fact]
    public void WebApplicationFactory_ShouldBeCreatable()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("STREAMFORGE_RUN_API_HOST_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var factory = new WebApplicationFactory<Program>();

        factory.Should().NotBeNull();
    }
}
