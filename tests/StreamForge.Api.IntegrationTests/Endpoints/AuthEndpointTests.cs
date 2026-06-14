using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Auth;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class AuthEndpointTests
{
    private readonly ApiIntegrationFixture _fixture;

    public AuthEndpointTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [ApiIntegrationFact]
    public async Task AuthEndpoints_ShouldRegisterLoginRefreshAndReturnCurrentUser()
    {
        await _fixture.ResetDatabaseAsync();

        var registered = await _fixture.Client.RegisterAsync("viewer@example.com");
        var meRequest = ApiTestClient.JsonRequest(HttpMethod.Get, "/api/v1/auth/me", accessToken: registered.AccessToken);
        var meResponse = await _fixture.Client.SendAsync(meRequest);
        var login = await _fixture.Client.LoginAsync("viewer@example.com");
        var refreshResponse = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequestDto(login.RefreshToken),
            ApiTestClient.JsonOptions);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentUser = await meResponse.Content.ReadFromJsonAsync<AuthUserDto>(ApiTestClient.JsonOptions);
        currentUser!.Email.Should().Be("viewer@example.com");
        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>(ApiTestClient.JsonOptions);
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [ApiIntegrationFact]
    public async Task AuthMe_ShouldRequireBearerToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
