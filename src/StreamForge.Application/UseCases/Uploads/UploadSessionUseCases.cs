using System.Security.Cryptography;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Uploads;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Uploads.CreateSession;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Uploads;

public sealed record GetUploadTargetCommand(Guid SessionId, int PartNumber, long PartSize);

public sealed record UploadPartCommand(
    Guid SessionId,
    int PartNumber,
    Stream FileStream,
    string FileName,
    long FileLength,
    string Checksum);

public sealed record CompleteUploadSessionCommand(Guid SessionId, string FileName);

public sealed class CreateUploadSessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly UploadOptions _uploadOptions;

    public CreateUploadSessionService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        UploadOptions uploadOptions)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadOptions = uploadOptions;
    }

    public async Task<CreateUploadSessionResponseDto> Handle(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated to create upload session");

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Video title is required", nameof(request.Title));
        }

        if (request.TotalSize <= 0)
        {
            throw new ArgumentException("File size must be greater than 0", nameof(request.TotalSize));
        }

        if (request.TotalSize > _uploadOptions.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed ({_uploadOptions.MaxFileSize} bytes)", nameof(request.TotalSize));
        }

        if (!string.IsNullOrWhiteSpace(request.ContentType) &&
            !_uploadOptions.AllowedMimeTypes.Contains(request.ContentType))
        {
            throw new ArgumentException($"MIME type '{request.ContentType}' is not allowed", nameof(request.ContentType));
        }

        if (request.CategoryId.HasValue && !await _unitOfWork.Categories.ExistsAsync(request.CategoryId.Value, cancellationToken))
        {
            throw new EntityNotFoundException("Category", request.CategoryId.Value);
        }

        var tagIds = request.TagIds?
            .Where(tagId => tagId != Guid.Empty)
            .Distinct()
            .ToArray() ?? Array.Empty<Guid>();

        foreach (var tagId in tagIds)
        {
            if (!await _unitOfWork.Tags.ExistsAsync(tagId, cancellationToken))
            {
                throw new EntityNotFoundException("Tag", tagId);
            }
        }

        var storageProviderType = Enum.TryParse<StorageProviderType>(_uploadOptions.StorageProviderType, true, out var parsedProvider)
            ? parsedProvider
            : StorageProviderType.Local;

        var video = Video.Create(
            title: request.Title,
            description: request.Description,
            uploaderId: userId,
            categoryId: request.CategoryId,
            visibility: request.Visibility ?? VideoVisibility.Private,
            status: VideoStatus.Uploading);

        var session = UploadSession.Create(
            userId: userId,
            videoId: video.Id,
            totalSize: request.TotalSize,
            storageProviderType: storageProviderType,
            temporaryStoragePath: Path.Combine("uploads", "sessions", Guid.NewGuid().ToString("N")),
            contentType: request.ContentType,
            sessionExpirationMinutes: _uploadOptions.SessionExpirationMinutes);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Videos.AddAsync(video, cancellationToken);
            foreach (var tagId in tagIds)
            {
                var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, cancellationToken)
                    ?? throw new EntityNotFoundException("Tag", tagId);
                tag.IncrementUsageCount();
                await _unitOfWork.VideoTags.AddAsync(VideoTag.Create(video.Id, tagId), cancellationToken);
            }

            await _unitOfWork.UploadSessions.AddAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new CreateUploadSessionResponseDto(session.Id, video.Id, session.ExpiresAt, video.Title);
    }
}

public sealed class GetUploadTargetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageService _storageService;
    private readonly UploadOptions _uploadOptions;

    public GetUploadTargetService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IStorageService storageService,
        UploadOptions uploadOptions)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _storageService = storageService;
        _uploadOptions = uploadOptions;
    }

    public async Task<UploadTargetDto> Handle(GetUploadTargetCommand command, CancellationToken cancellationToken)
    {
        ValidatePart(command.PartNumber, command.PartSize, _uploadOptions.ChunkSize);

        var session = await GetOwnedSessionAsync(command.SessionId, cancellationToken);
        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await MarkVideoFailedAsync(session.VideoId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Upload session has expired");
        }

        session.MarkAsActive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var target = await _storageService.GetUploadTargetAsync(
            command.SessionId,
            command.PartNumber,
            command.PartSize,
            cancellationToken);

        return new UploadTargetDto(target.Type.ToString(), target.Url, target.Headers, target.HttpMethod);
    }

    private async Task<UploadSession> GetOwnedSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.UploadSessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new EntityNotFoundException("UploadSession", sessionId);

        if (session.UserId != _currentUserService.UserId)
        {
            throw new System.UnauthorizedAccessException("You do not own this upload session");
        }

        return session;
    }

    private async Task MarkVideoFailedAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken);
        video?.MarkAsFailed();
    }

    private static void ValidatePart(int partNumber, long partSize, int maxChunkSize)
    {
        if (partNumber <= 0)
        {
            throw new ArgumentException("Part number must be greater than 0", nameof(partNumber));
        }

        if (partSize <= 0)
        {
            throw new ArgumentException("Part size must be greater than 0", nameof(partSize));
        }

        if (partSize > maxChunkSize)
        {
            throw new ArgumentException("Part size exceeds configured chunk size", nameof(partSize));
        }
    }
}

public sealed class UploadPartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageService _storageService;
    private readonly UploadOptions _uploadOptions;

    public UploadPartService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IStorageService storageService,
        UploadOptions uploadOptions)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _storageService = storageService;
        _uploadOptions = uploadOptions;
    }

    public async Task<UploadPartResponseDto> Handle(UploadPartCommand command, CancellationToken cancellationToken)
    {
        if (command.PartNumber <= 0)
        {
            throw new ArgumentException("Part number must be greater than 0", nameof(command.PartNumber));
        }

        if (command.FileLength <= 0)
        {
            throw new ArgumentException("Uploaded file part is required", nameof(command.FileLength));
        }

        if (command.FileLength > _uploadOptions.ChunkSize)
        {
            throw new ArgumentException("Uploaded part exceeds configured chunk size", nameof(command.FileLength));
        }

        if (string.IsNullOrWhiteSpace(command.Checksum))
        {
            throw new ArgumentException("Part checksum is required", nameof(command.Checksum));
        }

        var session = await GetOwnedSessionAsync(command.SessionId, cancellationToken);
        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await MarkVideoFailedAsync(session.VideoId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Upload session has expired");
        }

        var existingPart = await _unitOfWork.UploadSessionParts.GetBySessionAndPartAsync(command.SessionId, command.PartNumber, cancellationToken);
        if (existingPart is not null)
        {
            throw new DuplicateEntityException("UploadSessionPart", "PartNumber", command.PartNumber);
        }

        await using var buffer = new MemoryStream();
        await command.FileStream.CopyToAsync(buffer, cancellationToken);
        var actualChecksum = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
        if (!string.Equals(actualChecksum, command.Checksum.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Uploaded part checksum does not match the provided checksum", nameof(command.Checksum));
        }

        buffer.Position = 0;
        var storagePath = await _storageService.SavePartAsync(
            command.SessionId,
            command.PartNumber,
            buffer,
            command.FileName,
            cancellationToken);

        var part = UploadSessionPart.Create(command.SessionId, command.PartNumber, command.FileLength, actualChecksum, storagePath);
        part.MarkAsComplete();
        await _unitOfWork.UploadSessionParts.AddAsync(part, cancellationToken);

        session.UpdateUploadedSize(session.UploadedSize + command.FileLength);
        session.MarkAsActive();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadPartResponseDto(command.SessionId, command.PartNumber, part.IsComplete);
    }

    private async Task<UploadSession> GetOwnedSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.UploadSessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new EntityNotFoundException("UploadSession", sessionId);

        if (session.UserId != _currentUserService.UserId)
        {
            throw new System.UnauthorizedAccessException("You do not own this upload session");
        }

        return session;
    }

    private async Task MarkVideoFailedAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken);
        video?.MarkAsFailed();
    }
}

public sealed class CompleteUploadSessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageService _storageService;
    private readonly IVideoProcessingQueue _videoProcessingQueue;

    public CompleteUploadSessionService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IStorageService storageService,
        IVideoProcessingQueue videoProcessingQueue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _storageService = storageService;
        _videoProcessingQueue = videoProcessingQueue;
    }

    public async Task<CompleteUploadSessionResponseDto> Handle(CompleteUploadSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new ArgumentException("File name is required", nameof(command.FileName));
        }

        var session = await _unitOfWork.UploadSessions.GetByIdAsync(command.SessionId, cancellationToken)
            ?? throw new EntityNotFoundException("UploadSession", command.SessionId);

        if (session.UserId != _currentUserService.UserId)
        {
            throw new System.UnauthorizedAccessException("You do not own this upload session");
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await MarkVideoFailedAsync(session.VideoId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Upload session has expired");
        }

        var parts = (await _unitOfWork.UploadSessionParts.GetBySessionIdAsync(command.SessionId, cancellationToken)).ToList();
        ValidateParts(session, parts);

        session.MarkAsCompleting();
        var finalPath = await _storageService.AssembleChunksAsync(
            command.SessionId,
            parts.Select(part => part.StoragePath),
            Path.GetFileName(command.FileName),
            cancellationToken);

        var finalSize = await _storageService.GetFileSizeAsync(finalPath, cancellationToken);
        if (finalSize != session.TotalSize)
        {
            session.MarkAsFailed();
            await MarkVideoFailedAsync(session.VideoId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Assembled file size does not match upload session total size");
        }

        var storageProvider = await _unitOfWork.StorageProviders.GetDefaultByTypeAsync(session.StorageProviderType, cancellationToken)
            ?? throw new InvalidOperationException($"No active default storage provider configured for {session.StorageProviderType}");

        var video = await _unitOfWork.Videos.GetByIdAsync(session.VideoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", session.VideoId);

        var finalChecksum = await _storageService.CalculateChecksumAsync(finalPath, cancellationToken: cancellationToken);
        var permanentPath = await _storageService.PromoteCompletedUploadAsync(
            video.Id,
            finalPath,
            command.FileName,
            cancellationToken);

        var permanentSize = await _storageService.GetFileSizeAsync(permanentPath, cancellationToken);
        if (permanentSize != finalSize)
        {
            session.MarkAsFailed();
            await MarkVideoFailedAsync(session.VideoId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Promoted video source file size does not match assembled upload size");
        }

        var videoFormat = ResolveVideoFormat(command.FileName, session.ContentType);
        var version = VideoVersion.Create(
            videoId: video.Id,
            resolution: "original",
            format: videoFormat,
            storagePath: permanentPath,
            sizeBytes: finalSize,
            durationSeconds: 0);

        var videoFile = VideoFile.Create(
            videoVersionId: version.Id,
            storageProviderId: storageProvider.Id,
            filePath: permanentPath,
            fileSize: finalSize,
            mimeType: session.ContentType ?? "video/mp4",
            checksum: finalChecksum);
        var processingJob = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.VideoVersions.AddAsync(version, cancellationToken);
            await _unitOfWork.VideoFiles.AddAsync(videoFile, cancellationToken);
            await _unitOfWork.VideoProcessingJobs.AddAsync(processingJob, cancellationToken);

            video.MarkAsProcessing();
            session.MarkAsCompleted();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        try
        {
            await _storageService.DeleteRangeAsync(parts.Select(part => part.StoragePath).Append(finalPath), cancellationToken);
        }
        catch
        {
            // Storage providers should log cleanup details; cleanup should not fail a completed upload.
        }

        await _videoProcessingQueue.EnqueueAsync(processingJob.Id, cancellationToken);

        return new CompleteUploadSessionResponseDto(command.SessionId, video.Id, session.Status.ToString());
    }

    private static void ValidateParts(UploadSession session, IReadOnlyCollection<UploadSessionPart> parts)
    {
        if (parts.Count == 0)
        {
            throw new InvalidOperationException("No uploaded parts were found for this session");
        }

        var expectedPartNumber = 1;
        long totalSize = 0;
        foreach (var part in parts.OrderBy(part => part.PartNumber))
        {
            if (!part.IsComplete)
            {
                throw new InvalidOperationException($"Part {part.PartNumber} is not complete");
            }

            if (part.PartNumber != expectedPartNumber)
            {
                throw new InvalidOperationException($"Missing upload part {expectedPartNumber}");
            }

            totalSize += part.Size;
            expectedPartNumber++;
        }

        if (totalSize != session.TotalSize)
        {
            throw new InvalidOperationException("Uploaded part sizes do not match upload session total size");
        }
    }

    private static VideoFormat ResolveVideoFormat(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".webm" || string.Equals(contentType, "video/webm", StringComparison.OrdinalIgnoreCase))
        {
            return VideoFormat.WebM;
        }

        return VideoFormat.Mp4;
    }

    private async Task MarkVideoFailedAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken);
        video?.MarkAsFailed();
    }
}
