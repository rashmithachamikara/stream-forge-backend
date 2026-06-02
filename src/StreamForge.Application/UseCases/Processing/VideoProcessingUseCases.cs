using Microsoft.Extensions.Logging;
using StreamForge.Application.DTOs.Processing;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Processing;

public sealed class ProcessVideoJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaProcessingService _mediaProcessingService;
    private readonly ILogger<ProcessVideoJobService> _logger;

    public ProcessVideoJobService(
        IUnitOfWork unitOfWork,
        IMediaProcessingService mediaProcessingService,
        ILogger<ProcessVideoJobService> logger)
    {
        _unitOfWork = unitOfWork;
        _mediaProcessingService = mediaProcessingService;
        _logger = logger;
    }

    public async Task Handle(Guid processingJobId, CancellationToken cancellationToken = default)
    {
        var job = await _unitOfWork.VideoProcessingJobs.GetByIdAsync(processingJobId, cancellationToken)
            ?? throw new EntityNotFoundException("VideoProcessingJob", processingJobId);
        var video = await _unitOfWork.Videos.GetByIdAsync(job.VideoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", job.VideoId);
        var originalFile = await _unitOfWork.VideoFiles.GetOriginalByVideoIdAsync(video.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Video {video.Id} does not have an original source file");
        var originalVersion = await _unitOfWork.VideoVersions.GetByIdAsync(originalFile.VideoVersionId, cancellationToken)
            ?? throw new EntityNotFoundException("VideoVersion", originalFile.VideoVersionId);
        var storageProvider = await _unitOfWork.StorageProviders.GetDefaultByTypeAsync(StorageProviderType.Local, cancellationToken)
            ?? throw new InvalidOperationException("No active default local storage provider configured");

        try
        {
            _logger.LogInformation(
                "Video processing job {ProcessingJobId} started for video {VideoId}",
                job.Id,
                video.Id);

            if (job.Status == ProcessingJobStatus.Pending)
            {
                job.Start();
            }

            video.MarkAsProcessing();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var metadata = await _mediaProcessingService.ProbeAsync(originalFile.FilePath, cancellationToken);
            originalVersion.UpdateTechnicalMetadata(metadata.DurationSeconds, metadata.Bitrate, metadata.Codec);
            job.UpdateProgress(20);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Media probe completed for video {VideoId}: duration={DurationSeconds}s, resolution={Width}x{Height}, codec={Codec}, bitrate={BitrateKbps}kbps",
                video.Id,
                metadata.DurationSeconds,
                metadata.Width,
                metadata.Height,
                metadata.Codec,
                metadata.Bitrate);

            var hlsOutput = await _mediaProcessingService.GenerateHlsAsync(video.Id, originalFile.FilePath, metadata, cancellationToken);
            await AddHlsOutputsAsync(video.Id, storageProvider.Id, metadata, hlsOutput, cancellationToken);
            job.UpdateProgress(75);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "HLS generation completed for video {VideoId}: variants={VariantCount}, masterPlaylist={MasterPlaylistPath}",
                video.Id,
                hlsOutput.Variants.Count,
                hlsOutput.MasterPlaylistPath);

            var thumbnail = await _mediaProcessingService.GenerateThumbnailAsync(video.Id, originalFile.FilePath, metadata, cancellationToken);
            await _unitOfWork.VideoThumbnails.AddAsync(VideoThumbnail.Create(
                video.Id,
                thumbnail.StoragePath,
                thumbnail.Width,
                thumbnail.Height,
                thumbnail.SizeBytes,
                isDefault: true,
                timestampSeconds: thumbnail.TimestampSeconds), cancellationToken);
            _logger.LogInformation(
                "Thumbnail generated for video {VideoId}: path={ThumbnailPath}, timestamp={TimestampSeconds}s",
                video.Id,
                thumbnail.StoragePath,
                thumbnail.TimestampSeconds);

            job.UpdateProgress(95);
            job.Complete();
            video.MarkAsReady();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Video processing job {ProcessingJobId} completed for video {VideoId}",
                job.Id,
                video.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Video processing job {ProcessingJobId} failed for video {VideoId}",
                job.Id,
                video.Id);
            job.Fail(exception.Message);
            video.MarkAsFailed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task AddHlsOutputsAsync(
        Guid videoId,
        Guid storageProviderId,
        MediaProbeResult metadata,
        HlsOutputResult hlsOutput,
        CancellationToken cancellationToken)
    {
        var masterVersion = VideoVersion.Create(
            videoId,
            "adaptive",
            VideoFormat.HLS,
            hlsOutput.MasterPlaylistPath,
            hlsOutput.MasterPlaylistSize,
            metadata.DurationSeconds,
            codec: metadata.Codec);
        var masterFile = VideoFile.Create(
            masterVersion.Id,
            storageProviderId,
            hlsOutput.MasterPlaylistPath,
            hlsOutput.MasterPlaylistSize,
            "application/vnd.apple.mpegurl");

        await _unitOfWork.VideoVersions.AddAsync(masterVersion, cancellationToken);
        await _unitOfWork.VideoFiles.AddAsync(masterFile, cancellationToken);

        foreach (var variant in hlsOutput.Variants)
        {
            var version = VideoVersion.Create(
                videoId,
                variant.Resolution,
                VideoFormat.HLS,
                variant.PlaylistPath,
                variant.PlaylistSize,
                metadata.DurationSeconds,
                variant.Bitrate,
                variant.Codec ?? metadata.Codec);
            var file = VideoFile.Create(
                version.Id,
                storageProviderId,
                variant.PlaylistPath,
                variant.PlaylistSize,
                "application/vnd.apple.mpegurl");

            await _unitOfWork.VideoVersions.AddAsync(version, cancellationToken);
            await _unitOfWork.VideoFiles.AddAsync(file, cancellationToken);
        }
    }
}

public sealed class GetPlaybackManifestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStorageService _storageService;

    public GetPlaybackManifestService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _storageService = storageService;
    }

    public async Task<StoredFileDescriptor> Handle(Guid videoId, string? shareToken, CancellationToken cancellationToken)
    {
        await EnsureCanViewReadyVideoAsync(videoId, shareToken, cancellationToken);
        var versions = await _unitOfWork.VideoVersions.GetByVideoIdAsync(videoId, cancellationToken);
        var manifest = versions
            .Where(version => version.Format == VideoFormat.HLS)
            .OrderByDescending(version => version.Resolution == "adaptive")
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Video does not have a playable HLS manifest");

        var manifestFile = await _storageService.OpenReadAsync(manifest.StoragePath, "application/vnd.apple.mpegurl", cancellationToken);
        using var reader = new StreamReader(manifestFile.Stream);
        var manifestText = await reader.ReadToEndAsync(cancellationToken);
        var rewrittenManifest = RewriteMasterManifest(manifestText, shareToken);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rewrittenManifest));

        return new StoredFileDescriptor(stream, "application/vnd.apple.mpegurl", manifestFile.FileName, stream.Length);
    }

    private async Task EnsureCanViewReadyVideoAsync(Guid videoId, string? shareToken, CancellationToken cancellationToken)
    {
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video is not ready for playback");
        }

        var canView = await _authorizationService.CanViewVideoAsync(videoId, _currentUserService.UserId, _currentUserService.Role, shareToken, cancellationToken);
        if (!canView)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video");
        }
    }

    private static string RewriteMasterManifest(string manifestText, string? shareToken)
    {
        var queryString = string.IsNullOrWhiteSpace(shareToken)
            ? string.Empty
            : $"?shareToken={Uri.EscapeDataString(shareToken)}";
        var lines = manifestText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line =>
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || Uri.TryCreate(line, UriKind.Absolute, out _))
                {
                    return line;
                }

                var normalized = line.Replace('\\', '/').TrimStart('/');
                if (normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{normalized}{queryString}";
                }

                return $"assets/{normalized}{queryString}";
            });

        return string.Join('\n', lines);
    }
}

public sealed class GetStreamingAssetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStorageService _storageService;

    public GetStreamingAssetService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _storageService = storageService;
    }

    public async Task<StoredFileDescriptor> Handle(Guid videoId, string assetPath, string? shareToken, CancellationToken cancellationToken)
    {
        var normalizedAssetPath = Uri.UnescapeDataString(assetPath)
            .Replace('\\', '/')
            .TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalizedAssetPath) ||
            normalizedAssetPath.Split('/').Any(segment => segment == ".."))
        {
            throw new ArgumentException("Invalid streaming asset path", nameof(assetPath));
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video is not ready for playback");
        }

        var canView = await _authorizationService.CanViewVideoAsync(videoId, _currentUserService.UserId, _currentUserService.Role, shareToken, cancellationToken);
        if (!canView)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video");
        }

        var storagePath = Path.Combine("processing", videoId.ToString("N"), "hls", normalizedAssetPath);
        var contentType = normalizedAssetPath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            ? "application/vnd.apple.mpegurl"
            : "video/mp2t";

        return await _storageService.OpenReadAsync(storagePath, contentType, cancellationToken);
    }
}

public sealed class GetVideoThumbnailService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStorageService _storageService;

    public GetVideoThumbnailService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _storageService = storageService;
    }

    public async Task<StoredFileDescriptor> Handle(Guid videoId, string? shareToken, CancellationToken cancellationToken)
    {
        var canView = await _authorizationService.CanViewVideoAsync(videoId, _currentUserService.UserId, _currentUserService.Role, shareToken, cancellationToken);
        if (!canView)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video");
        }

        var thumbnail = await _unitOfWork.VideoThumbnails.GetDefaultByVideoIdAsync(videoId, cancellationToken)
            ?? throw new InvalidOperationException("Video does not have a default thumbnail");

        return await _storageService.OpenReadAsync(thumbnail.StoragePath, "image/jpeg", cancellationToken);
    }
}
