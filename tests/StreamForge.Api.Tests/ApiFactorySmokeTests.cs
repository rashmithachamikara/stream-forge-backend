using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StreamForge.Api.Tests;

public sealed class ApiFactorySmokeTests
{
    [Fact(Skip = "Requires a test database override before booting the full API host.")]
    public void WebApplicationFactory_ShouldBeCreatable()
    {
        using var factory = new WebApplicationFactory<Program>();

        factory.Should().NotBeNull();
    }
}
