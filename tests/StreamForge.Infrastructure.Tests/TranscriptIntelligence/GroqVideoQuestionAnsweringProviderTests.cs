using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Exceptions;
using StreamForge.Infrastructure.TranscriptIntelligence;

namespace StreamForge.Infrastructure.Tests.TranscriptIntelligence;

public sealed class GroqVideoQuestionAnsweringProviderTests
{
    [Fact]
    public async Task AnswerAsync_ShouldParseStructuredJsonResponse()
    {
        var chunkId = Guid.NewGuid();
        var responseJson =
            $$"""
              {
                "choices": [
                  {
                    "message": {
                      "content": "{\"canAnswer\":true,\"answer\":\"Grounded answer\",\"citations\":[\"{{chunkId}}\"]}"
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
            BaseAddress = new Uri("https://api.groq.com")
        };

        var provider = new GroqVideoQuestionAnsweringProvider(
            httpClient,
            new RagOptions
            {
                QaProviderConfigs = new RagQaProviderConfigs
                {
                    Groq = new RagGroqQaOptions
                    {
                        ApiKey = "test-key",
                        BaseUrl = "https://api.groq.com",
                        TimeoutSeconds = 60
                    }
                }
            },
            NullLogger<GroqVideoQuestionAnsweringProvider>.Instance);

        var result = await provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "groq",
                "llama-3.1-8b-instant",
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
                      "choices": [
                        {
                          "message": {
                            "content": "not-json"
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }))
        {
            BaseAddress = new Uri("https://api.groq.com")
        };

        var provider = new GroqVideoQuestionAnsweringProvider(
            httpClient,
            new RagOptions
            {
                QaProviderConfigs = new RagQaProviderConfigs
                {
                    Groq = new RagGroqQaOptions
                    {
                        ApiKey = "test-key",
                        BaseUrl = "https://api.groq.com",
                        TimeoutSeconds = 60
                    }
                }
            },
            NullLogger<GroqVideoQuestionAnsweringProvider>.Instance);

        var act = () => provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "groq",
                "llama-3.1-8b-instant",
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
                Content = new StringContent("{\"error\":\"rate limit\"}", Encoding.UTF8, "application/json")
            }))
        {
            BaseAddress = new Uri("https://api.groq.com")
        };

        var provider = new GroqVideoQuestionAnsweringProvider(
            httpClient,
            new RagOptions
            {
                QaProviderConfigs = new RagQaProviderConfigs
                {
                    Groq = new RagGroqQaOptions
                    {
                        ApiKey = "test-key",
                        BaseUrl = "https://api.groq.com",
                        TimeoutSeconds = 60,
                        Model = "llama-3.1-8b-instant"
                    }
                }
            },
            NullLogger<GroqVideoQuestionAnsweringProvider>.Instance);

        var act = () => provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                "groq",
                "llama-3.1-8b-instant",
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
}
