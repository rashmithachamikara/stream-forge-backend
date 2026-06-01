using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Uploads;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Data;
using StreamForge.Infrastructure.Storage;

namespace StreamForge.Api.Controllers.Uploads;

[ApiController]
[Route("api/v1/uploads/sessions")]
[Authorize]
public sealed class UploadSessionsController : ControllerBase
{
    private readonly StreamForgeDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOptions<UploadOptions> _uploadOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadSessionsController> _logger;

    public UploadSessionsController(
        StreamForgeDbContext dbContext,
        ICurrentUserService currentUserService,
        IOptions<UploadOptions> uploadOptions,
        IWebHostEnvironment environment,
        ILogger<UploadSessionsController> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _uploadOptions = uploadOptions;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUploadSessionResponseDto>> CreateSession(
        [FromBody] CreateUploadSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to create upload session");
        }

        if (request.TotalSize <= 0)
        {
            throw new ArgumentException("File size must be greater than 0", nameof(request.TotalSize));
        }

        if (request.TotalSize > _uploadOptions.Value.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed ({_uploadOptions.Value.MaxFileSize} bytes)", nameof(request.TotalSize));
        }

        if (!string.IsNullOrWhiteSpace(request.ContentType) &&
            !_uploadOptions.Value.AllowedMimeTypes.Contains(request.ContentType))
        {
            throw new ArgumentException($"MIME type '{request.ContentType}' is not allowed", nameof(request.ContentType));
        }

        var storageProviderType = Enum.TryParse<StorageProviderType>(_uploadOptions.Value.StorageProviderType, true, out var parsedProvider)
            ? parsedProvider
            : StorageProviderType.Local;

        var tempStoragePath = Path.Combine("uploads", "sessions", Guid.NewGuid().ToString("N"));

        var session = UploadSession.Create(
            userId: userId.Value,
            videoTitle: request.Title,
            totalSize: request.TotalSize,
            storageProviderType: storageProviderType,
            temporaryStoragePath: tempStoragePath,
            visibility: VideoVisibility.Private,
            videoDescription: request.Description,
            categoryId: request.CategoryId,
            contentType: request.ContentType,
            sessionExpirationMinutes: _uploadOptions.Value.SessionExpirationMinutes);

        _dbContext.UploadSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CreateUploadSessionResponseDto(session.Id, session.ExpiresAt, session.VideoTitle));
    }

    [HttpGet("{sessionId:guid}/target")]
    public async Task<ActionResult<UploadTargetDto>> GetUploadTarget(
        Guid sessionId,
        [FromQuery] int partNumber,
        [FromQuery] long partSize,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException($"Upload session {sessionId} not found");
        }

        if (session.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You do not own this upload session");
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Upload session has expired");
        }

        session.MarkAsActive();
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (session.StorageProviderType == StorageProviderType.S3)
        {
            var target = new UploadTargetDto(
                Type: StorageTargetType.S3PresignedUrl.ToString(),
                Url: $"https://s3.amazonaws.com/upload-session/{session.Id}/parts/{partNumber}?size={partSize}",
                Headers: new Dictionary<string, string>
                {
                    ["x-amz-meta-session-id"] = session.Id.ToString(),
                    ["x-amz-meta-part-number"] = partNumber.ToString()
                },
                HttpMethod: "PUT");

            return Ok(target);
        }

        var backendUrl = Url.ActionLink(
            action: nameof(UploadPart),
            controller: null,
            values: new { sessionId, partNumber }) ?? $"/api/v1/uploads/sessions/{sessionId}/parts/{partNumber}";

        return Ok(new UploadTargetDto(
            Type: StorageTargetType.BackendEndpoint.ToString(),
            Url: backendUrl,
            HttpMethod: "POST"));
    }

    [HttpPost("{sessionId:guid}/parts/{partNumber:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadPartResponseDto>> UploadPart(
        Guid sessionId,
        int partNumber,
        [FromForm] UploadPartFormRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException($"Upload session {sessionId} not found");
        }

        if (session.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You do not own this upload session");
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Upload session has expired");
        }

        if (request.File is null || request.File.Length <= 0)
        {
            throw new ArgumentException("Uploaded file part is required", nameof(request.File));
        }

        if (request.File.Length > _uploadOptions.Value.ChunkSize)
        {
            throw new ArgumentException("Uploaded part exceeds configured chunk size", nameof(request.File));
        }

        var tempDirectory = Path.Combine(_environment.ContentRootPath, _uploadOptions.Value.StoragePath, session.Id.ToString("N"), "parts");
        Directory.CreateDirectory(tempDirectory);

        var partPath = Path.Combine(tempDirectory, $"part_{partNumber:D5}.bin");
        await using (var stream = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await request.File.CopyToAsync(stream, cancellationToken);
        }

        var part = await _dbContext.UploadSessionParts
            .FirstOrDefaultAsync(p => p.UploadSessionId == sessionId && p.PartNumber == partNumber, cancellationToken);

        if (part is null)
        {
            part = UploadSessionPart.Create(sessionId, partNumber, request.File.Length, request.Checksum, partPath);
            _dbContext.UploadSessionParts.Add(part);
        }
        else
        {
            throw new InvalidOperationException($"Part {partNumber} already exists for session {sessionId}");
        }

        session.UpdateUploadedSize(session.UploadedSize + request.File.Length);
        session.MarkAsActive();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UploadPartResponseDto(sessionId, partNumber, true));
    }

    [HttpPost("{sessionId:guid}/complete")]
    public async Task<ActionResult<CompleteUploadSessionResponseDto>> CompleteSession(
        Guid sessionId,
        [FromBody] CompleteUploadSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException($"Upload session {sessionId} not found");
        }

        if (session.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException("You do not own this upload session");
        }

        var parts = await _dbContext.UploadSessionParts
            .Where(p => p.UploadSessionId == sessionId)
            .OrderBy(p => p.PartNumber)
            .ToListAsync(cancellationToken);

        if (parts.Count == 0)
        {
            throw new InvalidOperationException("No uploaded parts were found for this session");
        }

        session.MarkAsCompleting();

        var finalDirectory = Path.Combine(_environment.ContentRootPath, _uploadOptions.Value.StoragePath, session.Id.ToString("N"), "final");
        Directory.CreateDirectory(finalDirectory);

        var finalPath = Path.Combine(finalDirectory, Path.GetFileName(request.FileName));
        await using (var finalStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            foreach (var part in parts)
            {
                await using var partStream = new FileStream(part.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await partStream.CopyToAsync(finalStream, cancellationToken);
            }
        }

        var video = Video.Create(
            title: session.VideoTitle,
            description: session.VideoDescription,
            uploaderId: session.UserId,
            categoryId: session.CategoryId,
            visibility: session.VideoVisibility);

        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var version = VideoVersion.Create(
            videoId: video.Id,
            resolution: "original",
            format: VideoFormat.Mp4,
            storagePath: Path.GetRelativePath(_environment.ContentRootPath, finalPath),
            sizeBytes: new FileInfo(finalPath).Length,
            durationSeconds: 1);

        _dbContext.VideoVersions.Add(version);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var storageProvider = await _dbContext.StorageProviders.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No storage provider configured");

        var videoFile = VideoFile.Create(
            videoVersionId: version.Id,
            storageProviderId: storageProvider.Id,
            filePath: Path.GetRelativePath(_environment.ContentRootPath, finalPath),
            fileSize: new FileInfo(finalPath).Length,
            mimeType: session.ContentType ?? "video/mp4",
            checksum: parts.FirstOrDefault()?.Checksum);

        _dbContext.VideoFiles.Add(videoFile);
        session.MarkAsCompleted(video.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed upload session {SessionId} and created video {VideoId}", sessionId, video.Id);

        return Ok(new CompleteUploadSessionResponseDto(sessionId, video.Id, session.Status.ToString()));
    }
}
