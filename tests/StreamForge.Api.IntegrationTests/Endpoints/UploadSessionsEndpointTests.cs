using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Uploads;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class UploadSessionsEndpointTests
{
    private const string _sourceFileName = "source.mp4";
    private const string _sourceContentType = "video/mp4";

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
        byte[] sourceBytes = [0x00, 0x41, 0xFF, 0x10, 0x72, 0x05, 0x80];
        var session = await UploadInChunksAsync("Local upload integration test", sourceBytes, editor.AccessToken);

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
        var storedFile = await storage.OpenReadAsync(file.FilePath, _sourceContentType);
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

    [ApiIntegrationFact]
    public async Task UploadThroughPlayback_ShouldCompleteProcessingAndServeGeneratedMedia()
    {
        await _fixture.ResetDatabaseAsync();
        var editor = await _fixture.CreateUserAsync(UserRole.Editor, "processing-smoke@example.com");
        var accessToken = editor.AccessToken;
        var session = await UploadInChunksAsync("Upload through playback", [0x00, 0x41, 0xFF, 0x10, 0x72], accessToken);
        _fixture.EnqueuedProcessingJobIds.Should().ContainSingle();
        var jobId = _fixture.EnqueuedProcessingJobIds.Single();
        var client = _fixture.Client;
        var videoUrl = $"/api/v1/videos/{session.VideoId}";
        var statusUrl = $"{videoUrl}/processing-status";

        var pending = await client.ReadOkJsonAsync<VideoProcessingStatusDetailsDto>(HttpMethod.Get, statusUrl, accessToken);
        pending.ProcessingJobId.Should().Be(jobId);
        pending.JobStatus.Should().Be(nameof(ProcessingJobStatus.Pending));
        pending.VideoStatus.Should().Be(VideoStatus.Processing);

        using (var scope = _fixture.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<ProcessVideoJobService>();
            await processor.Handle(jobId);
        }

        // The status endpoint can reconcile persisted state, so verify what the processor saved first.
        await using (var context = _fixture.CreateContext())
        {
            var processed = await context.VideoProcessingJobs.Include(job => job.Video).SingleAsync(job => job.Id == jobId);
            processed.ErrorMessage.Should().BeNull();
            processed.Status.Should().Be(ProcessingJobStatus.Completed);
            processed.Video.Status.Should().Be(VideoStatus.Ready);
        }

        var completed = await client.ReadOkJsonAsync<VideoProcessingStatusDetailsDto>(HttpMethod.Get, statusUrl, accessToken);
        completed.ProcessingJobId.Should().Be(jobId);
        completed.JobStatus.Should().Be(nameof(ProcessingJobStatus.Completed));
        completed.VideoStatus.Should().Be(VideoStatus.Ready);
        completed.Progress.Should().Be(100);

        var manifestUrl = new Uri(client.BaseAddress!, $"{videoUrl}/playback/manifest");
        using var manifest = await client.SendRequestAsync(HttpMethod.Get, manifestUrl.PathAndQuery, accessToken);
        manifest.StatusCode.Should().Be(HttpStatusCode.OK);
        (manifest.Content.Headers.ContentType?.MediaType).Should().Be(PlaybackTestMedia.HlsContentType);
        var playlistReference = await ReadHlsReferenceAsync(manifest);
        playlistReference.Should().Be($"assets/{PlaybackTestMedia.PlaylistPath}");

        var playlistUrl = new Uri(manifestUrl, playlistReference);
        using var playlist = await client.SendRequestAsync(HttpMethod.Get, playlistUrl.PathAndQuery, accessToken);
        playlist.StatusCode.Should().Be(HttpStatusCode.OK);
        var segmentReference = await ReadHlsReferenceAsync(playlist);
        segmentReference.Should().Be(PlaybackTestMedia.SegmentFileName);

        var segmentUrl = new Uri(playlistUrl, segmentReference);
        using var segment = await client.SendRequestAsync(HttpMethod.Get, segmentUrl.PathAndQuery, accessToken);
        segment.StatusCode.Should().Be(HttpStatusCode.OK);
        (await segment.Content.ReadAsByteArrayAsync()).Should().Equal(PlaybackTestMedia.SegmentBytes);

        using var thumbnail = await client.SendRequestAsync(HttpMethod.Get, $"{videoUrl}/thumbnail", accessToken);
        thumbnail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await thumbnail.Content.ReadAsByteArrayAsync()).Should().Equal(PlaybackTestMedia.ThumbnailBytes);
    }

    private static async Task<string> ReadHlsReferenceAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        text.Should().StartWith("#EXTM3U\n");
        var references = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith('#'));
        return references.Should().ContainSingle().Which;
    }

    private async Task<CreateUploadSessionResponseDto> UploadInChunksAsync(string title, byte[] bytes, string accessToken)
    {
        var client = _fixture.Client;
        var session = await client.ReadOkJsonAsync<CreateUploadSessionResponseDto>(HttpMethod.Post, "/api/v1/uploads/sessions", accessToken,
            new CreateUploadSessionRequestDto(title, null, bytes.Length, _sourceContentType));
        session.SessionId.Should().NotBeEmpty();
        session.VideoId.Should().NotBeEmpty();
        session.VideoTitle.Should().Be(title);

        var sessionUrl = $"/api/v1/uploads/sessions/{session.SessionId}";
        var split = bytes.Length / 2;
        await UploadPartAsync(session.SessionId, sessionUrl, 2, bytes[split..], accessToken);
        await UploadPartAsync(session.SessionId, sessionUrl, 1, bytes[..split], accessToken);

        var completion = await client.ReadOkJsonAsync<CompleteUploadSessionResponseDto>(HttpMethod.Post, $"{sessionUrl}/complete", accessToken,
            new CompleteUploadSessionRequestDto(session.SessionId, _sourceFileName));
        completion.Should().Be(new CompleteUploadSessionResponseDto(session.SessionId, session.VideoId, nameof(UploadSessionStatus.Completed)));
        return session;
    }

    private async Task UploadPartAsync(Guid sessionId, string sessionUrl, int partNumber, byte[] bytes, string accessToken)
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
        form.Add(new ByteArrayContent(bytes), "file", _sourceFileName);
        form.Add(new StringContent(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()), "checksum");

        using var uploadResponse = await _fixture.Client.SendAsync(uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var part = await uploadResponse.Content.ReadFromJsonAsync<UploadPartResponseDto>(ApiTestClient.JsonOptions);
        part.Should().Be(new UploadPartResponseDto(sessionId, partNumber, IsComplete: true));
    }
}
