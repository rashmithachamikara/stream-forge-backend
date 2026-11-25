using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Uploads;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class UploadSessionsEndpointTests
{
    private readonly ApiIntegrationFixture _fixture;

    public UploadSessionsEndpointTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [ApiIntegrationFact]
    public async Task LocalUpload_ShouldAssembleChunksAndEnqueueTranscodeJob()
    {
        await _fixture.ResetDatabaseAsync();
        var editor = await _fixture.CreateUserAsync(UserRole.Editor, "upload-editor@example.com");
        const string title = "Local upload integration test";
        const string fileName = "source.mp4";
        const string contentType = "video/mp4";
        byte[] sourceBytes = [0x00, 0x41, 0xFF, 0x10, 0x72, 0x05, 0x80];

        using var createRequest = ApiTestClient.JsonRequest(
            HttpMethod.Post,
            "/api/v1/uploads/sessions",
            new CreateUploadSessionRequestDto(title, null, sourceBytes.Length, contentType),
            editor.AccessToken);
        using var createResponse = await _fixture.Client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await createResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponseDto>(ApiTestClient.JsonOptions);
        session.Should().NotBeNull();
        session!.SessionId.Should().NotBeEmpty();
        session.VideoId.Should().NotBeEmpty();
        session.VideoTitle.Should().Be(title);

        var sessionUrl = $"/api/v1/uploads/sessions/{session.SessionId}";
        await UploadPartAsync(session.SessionId, sessionUrl, 2, sourceBytes[3..], fileName, editor.AccessToken);
        await UploadPartAsync(session.SessionId, sessionUrl, 1, sourceBytes[..3], fileName, editor.AccessToken);

        using var completeRequest = ApiTestClient.JsonRequest(
            HttpMethod.Post,
            $"{sessionUrl}/complete",
            new CompleteUploadSessionRequestDto(session.SessionId, fileName),
            editor.AccessToken);
        using var completeResponse = await _fixture.Client.SendAsync(completeRequest);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completion = await completeResponse.Content.ReadFromJsonAsync<CompleteUploadSessionResponseDto>(ApiTestClient.JsonOptions);
        completion.Should().Be(new CompleteUploadSessionResponseDto(session.SessionId, session.VideoId, nameof(UploadSessionStatus.Completed)));

        await using var context = _fixture.CreateContext();
        var savedSession = await context.UploadSessions.SingleAsync(item => item.Id == session.SessionId);
        savedSession.Status.Should().Be(UploadSessionStatus.Completed);
        savedSession.VideoId.Should().Be(session.VideoId);

        var video = await context.Videos.SingleAsync(item => item.Id == session.VideoId);
        video.Status.Should().Be(VideoStatus.Processing);
        var version = await context.VideoVersions.SingleAsync(item => item.VideoId == video.Id && item.Resolution == "original");
        var file = await context.VideoFiles.SingleAsync(item => item.VideoVersionId == version.Id);
        file.FileSize.Should().Be(sourceBytes.Length);
        file.Checksum.Should().Be(Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant());

        using var scope = _fixture.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var storedFile = await storage.OpenReadAsync(file.FilePath, contentType);
        await using var storedStream = storedFile.Stream;
        using var storedBytes = new MemoryStream();
        await storedStream.CopyToAsync(storedBytes);
        storedBytes.ToArray().Should().Equal(sourceBytes);

        var parts = await context.UploadSessionParts.Where(item => item.UploadSessionId == session.SessionId).ToListAsync();
        parts.Should().HaveCount(2);
        foreach (var part in parts)
        {
            (await storage.ExistsAsync(part.StoragePath)).Should().BeFalse("completed uploads remove their temporary chunk files");
        }

        var job = await context.VideoProcessingJobs.SingleAsync(item => item.VideoId == video.Id);
        job.JobType.Should().Be(ProcessingJobType.Transcode);
        job.Status.Should().Be(ProcessingJobStatus.Pending);
        _fixture.EnqueuedProcessingJobIds.Should().Equal(job.Id);
    }

    private async Task UploadPartAsync(Guid sessionId, string sessionUrl, int partNumber, byte[] bytes, string fileName, string accessToken)
    {
        using var targetRequest = ApiTestClient.JsonRequest(
            HttpMethod.Get,
            $"{sessionUrl}/target?partNumber={partNumber}&partSize={bytes.Length}",
            accessToken: accessToken);
        using var targetResponse = await _fixture.Client.SendAsync(targetRequest);
        targetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var target = await targetResponse.Content.ReadFromJsonAsync<UploadTargetDto>(ApiTestClient.JsonOptions);
        target.Should().NotBeNull();
        target!.Type.Should().Be(nameof(StorageTargetType.BackendEndpoint));
        target.Url.Should().Be($"{sessionUrl}/parts/{partNumber}");
        target.HttpMethod.Should().Be(HttpMethod.Post.Method);

        using var uploadRequest = ApiTestClient.JsonRequest(new HttpMethod(target.HttpMethod!), target.Url, accessToken: accessToken);
        var form = new MultipartFormDataContent();
        uploadRequest.Content = form;
        form.Add(new ByteArrayContent(bytes), "file", fileName);
        form.Add(new StringContent(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()), "checksum");

        using var uploadResponse = await _fixture.Client.SendAsync(uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var part = await uploadResponse.Content.ReadFromJsonAsync<UploadPartResponseDto>(ApiTestClient.JsonOptions);
        part.Should().Be(new UploadPartResponseDto(sessionId, partNumber, IsComplete: true));
    }
}
