using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.TranscriptIntelligence;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class AdminRagSettingsEndpointTests
{
    private readonly ApiIntegrationFixture _fixture;

    public AdminRagSettingsEndpointTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [ApiIntegrationFact]
    public async Task RagSettingsEndpoint_ShouldRequireAdminAndReturnModelCatalog()
    {
        await _fixture.ResetDatabaseAsync();
        var viewer = await _fixture.CreateUserAsync(UserRole.Viewer, "viewer-rag-settings@example.com");
        var admin = await _fixture.CreateUserAsync(UserRole.Admin, "admin-rag-settings@example.com");

        var forbidden = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Get,
            "/api/v1/admin/settings/rag",
            accessToken: viewer.AccessToken));

        var okResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Get,
            "/api/v1/admin/settings/rag",
            accessToken: admin.AccessToken));

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        okResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await okResponse.Content.ReadFromJsonAsync<AdminRagSettingsDto>(ApiTestClient.JsonOptions);
        result.Should().NotBeNull();
        result!.QaModelCatalog.Should().Contain(entry => entry.Provider == "gemini" && entry.Models.Contains("gemini-3.1-flash-lite"));
        result.QaModelCatalog.Should().Contain(entry => entry.Provider == "groq" && entry.Models.Contains("llama-3.3-70b-versatile"));
        result.GeminiQaModel.Should().NotBeNullOrWhiteSpace();
        result.GroqQaModel.Should().NotBeNullOrWhiteSpace();
    }

    [ApiIntegrationFact]
    public async Task RagSettingsEndpoint_ShouldRoundTripProviderModelsAndKeepSecretsMasked()
    {
        await _fixture.ResetDatabaseAsync();
        var admin = await _fixture.CreateUserAsync(UserRole.Admin, "admin-rag-roundtrip@example.com");

        var updateRequest = new UpdateAdminRagSettingsRequestDto(
            true,
            true,
            true,
            true,
            "local-sentence-transformer",
            "sentence-transformers/all-MiniLM-L6-v2",
            64,
            "hybrid",
            12,
            10,
            0.65d,
            0.35d,
            20,
            "groq",
            "gemini-2.5-pro",
            "grok-3",
            "openai/gpt-oss-20b",
            6,
            4,
            0.1d,
            768,
            "gemini-test-secret-1234",
            false,
            null,
            false,
            "groq-test-secret-5678",
            false);

        var updateResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Put,
            "/api/v1/admin/settings/rag",
            updateRequest,
            admin.AccessToken));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminRagSettingsDto>(ApiTestClient.JsonOptions);
        updated.Should().NotBeNull();
        updated!.QaProvider.Should().Be("groq");
        updated.GeminiQaModel.Should().Be("gemini-2.5-pro");
        updated.GrokQaModel.Should().Be("grok-3");
        updated.GroqQaModel.Should().Be("openai/gpt-oss-20b");
        updated.GeminiApiKey.IsConfigured.Should().BeTrue();
        updated.GeminiApiKey.MaskedValue.Should().NotBe("gemini-test-secret-1234");
        updated.GroqApiKey.IsConfigured.Should().BeTrue();
        updated.GroqApiKey.MaskedValue.Should().NotBe("groq-test-secret-5678");

        var getResponse = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Get,
            "/api/v1/admin/settings/rag",
            accessToken: admin.AccessToken));

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<AdminRagSettingsDto>(ApiTestClient.JsonOptions);
        fetched.Should().NotBeNull();
        fetched!.GeminiQaModel.Should().Be("gemini-2.5-pro");
        fetched.GrokQaModel.Should().Be("grok-3");
        fetched.GroqQaModel.Should().Be("openai/gpt-oss-20b");

        await using var context = _fixture.CreateContext();
        var systemSettings = await context.SystemSettings
            .Where(setting =>
                setting.Key == "rag.qa.providers.gemini.model" ||
                setting.Key == "rag.qa.providers.grok.model" ||
                setting.Key == "rag.qa.providers.groq.model")
            .ToListAsync();
        var systemSecrets = await context.SystemSecrets
            .Where(secret =>
                secret.Key == "rag.qa.providers.gemini.apiKey" ||
                secret.Key == "rag.qa.providers.groq.apiKey")
            .ToListAsync();

        systemSettings.Should().Contain(setting => setting.Key == "rag.qa.providers.gemini.model" && setting.Value == "gemini-2.5-pro");
        systemSettings.Should().Contain(setting => setting.Key == "rag.qa.providers.grok.model" && setting.Value == "grok-3");
        systemSettings.Should().Contain(setting => setting.Key == "rag.qa.providers.groq.model" && setting.Value == "openai/gpt-oss-20b");
        systemSecrets.Should().Contain(secret => secret.Key == "rag.qa.providers.gemini.apiKey" && secret.EncryptedValue != "gemini-test-secret-1234");
        systemSecrets.Should().Contain(secret => secret.Key == "rag.qa.providers.groq.apiKey" && secret.EncryptedValue != "groq-test-secret-5678");
    }

    [ApiIntegrationFact]
    public async Task RagSettingsEndpoint_ShouldRejectUnsupportedProviderModel()
    {
        await _fixture.ResetDatabaseAsync();
        var admin = await _fixture.CreateUserAsync(UserRole.Admin, "admin-rag-invalid-model@example.com");

        var updateRequest = new UpdateAdminRagSettingsRequestDto(
            true,
            true,
            true,
            true,
            "local-sentence-transformer",
            "sentence-transformers/all-MiniLM-L6-v2",
            64,
            "hybrid",
            12,
            10,
            0.65d,
            0.35d,
            20,
            "groq",
            null,
            null,
            "not-a-real-groq-model",
            6,
            4,
            0.1d,
            768,
            null,
            false,
            null,
            false,
            null,
            false);

        var response = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Put,
            "/api/v1/admin/settings/rag",
            updateRequest,
            admin.AccessToken));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("Unsupported model");
        error.Error.Should().Contain("groq");
    }

    private sealed record ErrorResponse(string Error, int StatusCode, DateTime Timestamp);
}
