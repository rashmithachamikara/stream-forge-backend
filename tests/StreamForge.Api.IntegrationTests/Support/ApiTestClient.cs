using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using StreamForge.Application.DTOs.Auth;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Api.IntegrationTests.Support;

internal static class ApiTestClient
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<AuthResponseDto> RegisterAsync(this HttpClient client, string email, string password = "Password123!")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto("Test User", email, password), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions))!;
    }

    public static async Task<AuthResponseDto> LoginAsync(this HttpClient client, string email, string password = "Password123!")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, password), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions))!;
    }

    public static async Task<AuthResponseDto> CreateUserAsync(
        this ApiIntegrationFixture fixture,
        UserRole role,
        string email,
        string password = "Password123!")
    {
        var registered = await fixture.Client.RegisterAsync(email, password);

        if (role != UserRole.Viewer)
        {
            await using var context = fixture.CreateContext();
            var user = await context.Users.FindAsync(registered.User.Id);
            user!.ChangeRole(role);
            await context.SaveChangesAsync();
        }

        return role == UserRole.Viewer
            ? registered
            : await fixture.Client.LoginAsync(email, password);
    }

    public static HttpRequestMessage JsonRequest(HttpMethod method, string url, object? body = null, string? accessToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    public static async Task<HttpResponseMessage> SendRequestAsync(
        this HttpClient client, HttpMethod method, string url, string? accessToken = null, object? body = null)
    {
        using var request = JsonRequest(method, url, body, accessToken);
        return await client.SendAsync(request);
    }

    public static async Task<T> ReadOkJsonAsync<T>(
        this HttpClient client, HttpMethod method, string url, string? accessToken = null, object? body = null)
        where T : class
    {
        using var response = await client.SendRequestAsync(method, url, accessToken, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"{method} {url} should succeed");
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new InvalidOperationException($"{method} {url} returned no JSON body.");
    }

    public static async Task AssertStatusAsync(
        this HttpClient client, HttpStatusCode status, HttpMethod method, string url, string? accessToken = null, object? body = null)
    {
        using var response = await client.SendRequestAsync(method, url, accessToken, body);
        response.StatusCode.Should().Be(status, $"{method} {url} should return {status}");
    }
}
