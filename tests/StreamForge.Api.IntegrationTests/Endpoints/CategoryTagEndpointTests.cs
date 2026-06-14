using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Content;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class CategoryTagEndpointTests
{
    private readonly ApiIntegrationFixture _fixture;

    public CategoryTagEndpointTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [ApiIntegrationFact]
    public async Task CategoryEndpoints_ShouldRequireAdminAndSupportCrud()
    {
        await _fixture.ResetDatabaseAsync();
        var viewer = await _fixture.CreateUserAsync(UserRole.Viewer, "viewer-category@example.com");
        var admin = await _fixture.CreateUserAsync(UserRole.Admin, "admin-category@example.com");

        var forbidden = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Post,
            "/api/v1/categories",
            new CreateCategoryRequestDto("Nope", null, null, 0),
            viewer.AccessToken));
        var createResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Post,
            "/api/v1/categories",
            new CreateCategoryRequestDto("API Tests", "Created from API tests", null, 99),
            admin.AccessToken));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>(ApiTestClient.JsonOptions);
        var updateResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Patch,
            $"/api/v1/categories/{created!.Id}",
            new UpdateCategoryRequestDto("API Tests Updated", null, null, false, 100, ClearDescription: true),
            admin.AccessToken));
        var deleteResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Delete,
            $"/api/v1/categories/{created.Id}",
            accessToken: admin.AccessToken));

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Name.Should().Be("API Tests");
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryDto>(ApiTestClient.JsonOptions);
        updated!.Name.Should().Be("API Tests Updated");
        updated.Description.Should().BeNull();
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [ApiIntegrationFact]
    public async Task TagEndpoints_ShouldRequireAdminAndSupportCrud()
    {
        await _fixture.ResetDatabaseAsync();
        var viewer = await _fixture.CreateUserAsync(UserRole.Viewer, "viewer-tag@example.com");
        var admin = await _fixture.CreateUserAsync(UserRole.Admin, "admin-tag@example.com");

        var forbidden = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Post,
            "/api/v1/tags",
            new CreateTagRequestDto("nope"),
            viewer.AccessToken));
        var createResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Post,
            "/api/v1/tags",
            new CreateTagRequestDto("API Tag"),
            admin.AccessToken));
        var created = await createResponse.Content.ReadFromJsonAsync<TagSummaryDto>(ApiTestClient.JsonOptions);
        var listResponse = await _fixture.Client.GetAsync("/api/v1/tags?search=api&page=1&pageSize=10");
        var updateResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tags/{created!.Id}",
            new UpdateTagRequestDto("Renamed API Tag"),
            admin.AccessToken));
        var deleteResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Delete,
            $"/api/v1/tags/{created.Id}",
            accessToken: admin.AccessToken));

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Name.Should().Be("api tag");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TagSummaryDto>(ApiTestClient.JsonOptions);
        updated!.Name.Should().Be("renamed api tag");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
