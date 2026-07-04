using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.TranscriptIntelligence;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.TranscriptIntelligence;

namespace StreamForge.Infrastructure.Tests.TranscriptIntelligence;

public sealed class GeminiVideoQuestionAnsweringProviderTests
{
    [Fact]
    public async Task AnswerAsync_ShouldParseStructuredJsonResponse()
    {
        var chunkId = Guid.NewGuid();
        var responseJson =
            $$"""
              {
                "candidates": [
                  {
                    "content": {
                      "parts": [
                        {
                          "text": "{\"canAnswer\":true,\"answer\":\"Grounded answer\",\"citations\":[\"{{chunkId}}\"]}"
                        }
                      ]
                    }
                  }
                ]
              }
              """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseJson,
                    Encoding.UTF8,
                    "application/json")
            }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com")
        };

        var provider = new GeminiVideoQuestionAnsweringProvider(
            httpClient,
            CreateSettingsResolver("test-key"),
            NullLogger<GeminiVideoQuestionAnsweringProvider>.Instance);

        var result = await provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "gemini",
                "gemini-2.5-flash",
                "What happened?",
                [
                    new GroundedQuestionEvidenceChunk(
                        chunkId,
                        Guid.NewGuid(),
                        "Video",
                        Guid.NewGuid(),
                        "en",
                        5,
                        10,
                        "Grounding evidence")
                ],
                3,
                256,
                0d),
            CancellationToken.None);

        result.CanAnswer.Should().BeTrue();
        result.Answer.Should().Be("Grounded answer");
        result.CitedChunkIds.Should().ContainSingle().Which.Should().Be(chunkId);
    }

    [Fact]
    public async Task AnswerAsync_ShouldRejectMalformedStructuredJson()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "candidates": [
                        {
                          "content": {
                            "parts": [
                              {
                                "text": "not-json"
                              }
                            ]
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com")
        };

        var provider = new GeminiVideoQuestionAnsweringProvider(
            httpClient,
            CreateSettingsResolver("test-key"),
            NullLogger<GeminiVideoQuestionAnsweringProvider>.Instance);

        var act = () => provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "gemini",
                "gemini-2.5-flash",
                "What happened?",
                [
                    new GroundedQuestionEvidenceChunk(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Video",
                        Guid.NewGuid(),
                        "en",
                        5,
                        10,
                        "Grounding evidence")
                ],
                3,
                256,
                0d),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid structured JSON*");
    }

    [Fact]
    public async Task AnswerAsync_ShouldThrowExternalServiceThrottledExceptionFor429()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("{\"error\":\"quota exceeded\"}", Encoding.UTF8, "application/json")
            }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com")
        };

        var provider = new GeminiVideoQuestionAnsweringProvider(
            httpClient,
            CreateSettingsResolver("test-key"),
            NullLogger<GeminiVideoQuestionAnsweringProvider>.Instance);

        var act = () => provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "gemini",
                "gemini-2.5-flash",
                "What happened?",
                [
                    new GroundedQuestionEvidenceChunk(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Video",
                        Guid.NewGuid(),
                        "en",
                        5,
                        10,
                        "Grounding evidence")
                ],
                3,
                256,
                0d),
            CancellationToken.None);

        await act.Should().ThrowAsync<ExternalServiceThrottledException>()
            .WithMessage("*rate-limiting requests*");
    }

    [Fact]
    public async Task AnswerAsync_ShouldPromptForProvidedVideosWhenEvidenceSpansMultipleVideos()
    {
        string? capturedRequestBody = null;
        var chunkId = Guid.NewGuid();
        var videoIdOne = Guid.NewGuid();
        var videoIdTwo = Guid.NewGuid();

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
            {
                capturedRequestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""
                          {
                            "candidates": [
                              {
                                "content": {
                                  "parts": [
                                    {
                                      "text": "{\"canAnswer\":true,\"answer\":\"Grounded answer\",\"citations\":[\"{{chunkId}}\"]}"
                                    }
                                  ]
                                }
                              }
                            ]
                          }
                          """,
                        Encoding.UTF8,
                        "application/json")
                };
            }))
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com")
        };

        var provider = new GeminiVideoQuestionAnsweringProvider(
            httpClient,
            CreateSettingsResolver("test-key"),
            NullLogger<GeminiVideoQuestionAnsweringProvider>.Instance);

        await provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "gemini",
                "gemini-2.5-flash",
                "What happened?",
                [
                    new GroundedQuestionEvidenceChunk(
                        chunkId,
                        videoIdOne,
                        "Video One",
                        Guid.NewGuid(),
                        "en",
                        5,
                        10,
                        "First evidence"),
                    new GroundedQuestionEvidenceChunk(
                        Guid.NewGuid(),
                        videoIdTwo,
                        "Video Two",
                        Guid.NewGuid(),
                        "en",
                        12,
                        18,
                        "Second evidence")
                ],
                3,
                256,
                0d),
            CancellationToken.None);

        capturedRequestBody.Should().NotBeNull();
        var normalizedRequestBody = capturedRequestBody!.Replace("\\u0022", "\"", StringComparison.Ordinal);
        normalizedRequestBody.Should().Contain("prefer phrases like \"the provided videos\"");
        normalizedRequestBody.Should().NotContain("prefer phrases like \"the provided video\" or");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request));
    }

    private static ResolveRagSettingsService CreateSettingsResolver(string apiKey)
    {
        var unitOfWork = new FixedUnitOfWork();
        return new ResolveRagSettingsService(unitOfWork, new RagOptions(), new FixedSecretStoreService(apiKey));
    }

    private sealed class FixedUnitOfWork : IUnitOfWork
    {
        public IUserRepository Users => throw new NotSupportedException();
        public IVideoRepository Videos => throw new NotSupportedException();
        public IVideoVersionRepository VideoVersions => throw new NotSupportedException();
        public IVideoFileRepository VideoFiles => throw new NotSupportedException();
        public IVideoThumbnailRepository VideoThumbnails => throw new NotSupportedException();
        public IVideoProcessingJobRepository VideoProcessingJobs => throw new NotSupportedException();
        public IVideoTranscriptionRepository VideoTranscriptions => throw new NotSupportedException();
        public IVideoTranscriptChunkRepository VideoTranscriptChunks => throw new NotSupportedException();
        public IVideoReactionRepository VideoReactions => throw new NotSupportedException();
        public IVideoCommentRepository VideoComments => throw new NotSupportedException();
        public IBookmarkRepository Bookmarks => throw new NotSupportedException();
        public IVideoTagRepository VideoTags => throw new NotSupportedException();
        public IStorageProviderRepository StorageProviders => throw new NotSupportedException();
        public ICategoryRepository Categories => throw new NotSupportedException();
        public ITagRepository Tags => throw new NotSupportedException();
        public IPlaylistRepository Playlists => throw new NotSupportedException();
        public IPlaylistVideoRepository PlaylistVideos => throw new NotSupportedException();
        public INotificationRepository Notifications => throw new NotSupportedException();
        public IAnalyticsEventRepository AnalyticsEvents => throw new NotSupportedException();
        public IUploadSessionRepository UploadSessions => throw new NotSupportedException();
        public IUploadSessionPartRepository UploadSessionParts => throw new NotSupportedException();
        public ISystemSettingRepository SystemSettings { get; } = new EmptySystemSettingRepository();
        public ISystemSecretRepository SystemSecrets => throw new NotSupportedException();
        public IAccessControlRepository AccessControls => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class EmptySystemSettingRepository : ISystemSettingRepository
    {
        public Task<SystemSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SystemSetting?>(null);
        public Task<IEnumerable<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<SystemSetting>>([]);
        public Task<SystemSetting> AddAsync(SystemSetting entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(SystemSetting entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(SystemSetting entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<SystemSetting?>(null);
        public Task<IReadOnlyList<SystemSetting>> GetByKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SystemSetting>>([]);
    }

    private sealed class FixedSecretStoreService : ISystemSecretStoreService
    {
        private readonly string _apiKey;

        public FixedSecretStoreService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public Task<string?> ResolveAsync(string key, string? fallbackValue, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(_apiKey);

        public Task<SecretConfigurationStatus> GetStatusAsync(string key, string? fallbackValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SecretConfigurationStatus(true, "****key"));

        public Task SetAsync(string key, string plaintextValue, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ClearAsync(string key, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
