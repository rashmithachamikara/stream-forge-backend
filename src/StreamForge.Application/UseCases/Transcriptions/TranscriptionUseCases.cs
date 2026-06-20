using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Transcriptions;

public sealed class StartVideoTranscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly TranscriptionOptions _options;
    private readonly ILogger<StartVideoTranscriptionService> _logger;

    public StartVideoTranscriptionService(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider,
        TranscriptionOptions options,
        ILogger<StartVideoTranscriptionService> logger)
    {
        _unitOfWork = unitOfWork;
        _transcriptionProvider = transcriptionProvider;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VideoTranscriptionDto>> Handle(Guid videoId, string? language, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Transcription is disabled.");
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video must be ready before transcription can start.");
        }

        var activeRecords = await _unitOfWork.VideoTranscriptions.GetByVideoAndStatusAsync(
            videoId,
            TranscriptionStatus.Pending,
            TranscriptionStatus.Processing);
        if (activeRecords.Count > 0)
        {
            return activeRecords.Select(TranscriptionMapper.ToDto).ToArray();
        }

        var originalFile = await _unitOfWork.VideoFiles.GetOriginalByVideoIdAsync(videoId, cancellationToken)
            ?? throw new InvalidOperationException($"Video {videoId} does not have an original source file.");

        var requestedLanguage = NormalizeLanguage(language) ?? NormalizeLanguage(_options.DefaultLanguage) ?? "auto";
        var outputFormats = NormalizeOutputFormats(_options.OutputFormats);
        var transcriptions = new List<VideoTranscription>(outputFormats.Count);

        foreach (var format in outputFormats)
        {
            var destinationStoragePath = BuildCanonicalStoragePath(videoId, requestedLanguage, format);
            var existing = await _unitOfWork.VideoTranscriptions.GetByVideoLanguageAndFormatAsync(
                videoId,
                requestedLanguage,
                format,
                cancellationToken);

            if (existing is null)
            {
                existing = VideoTranscription.Create(
                    videoId,
                    requestedLanguage,
                    format,
                    destinationStoragePath,
                    _options.Provider);
                await _unitOfWork.VideoTranscriptions.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.QueueForProcessing(destinationStoragePath, _options.Provider);
            }

            transcriptions.Add(existing);
        }

        var callbackUrl = $"{_options.CallbackBaseUrl.TrimEnd('/')}/internal/transcriptions/callback";
        var correlationId = $"{videoId:N}:{requestedLanguage}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        try
        {
            var result = await _transcriptionProvider.SubmitAsync(
                new TranscriptionProviderRequest(
                    videoId,
                    correlationId,
                    "local_path",
                    originalFile.FilePath,
                    requestedLanguage == "auto" ? null : requestedLanguage,
                    outputFormats.Select(format => format.ToLowerInvariant()).ToArray(),
                    callbackUrl,
                    _options.WorkerCallbackSecret,
                    _options.LocalFasterWhisper.Model,
                    _options.LocalFasterWhisper.Device,
                    _options.LocalFasterWhisper.ComputeType,
                    _options.LocalFasterWhisper.BeamSize,
                    _options.LocalFasterWhisper.EnableVad,
                    _options.LocalFasterWhisper.EnableWordTimestamps),
                cancellationToken);

            foreach (var transcription in transcriptions)
            {
                transcription.StartProcessing();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transcription submitted for video {VideoId}. Correlation={CorrelationId}, workerJobId={WorkerJobId}, formats={Formats}",
                videoId,
                correlationId,
                result.JobId,
                string.Join(", ", outputFormats));

            return transcriptions.Select(TranscriptionMapper.ToDto).ToArray();
        }
        catch (Exception exception)
        {
            foreach (var transcription in transcriptions)
            {
                transcription.Fail();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError(exception, "Failed to submit transcription for video {VideoId}", videoId);
            throw;
        }
    }

    private static string BuildCanonicalStoragePath(Guid videoId, string language, string format)
    {
        var fileName = format.Equals("vtt", StringComparison.OrdinalIgnoreCase)
            ? "captions.vtt"
            : "captions.srt";
        return Path.Combine("videos", videoId.ToString("N"), "transcriptions", language, fileName);
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        return language.Trim().ToLowerInvariant();
    }

    private static List<string> NormalizeOutputFormats(IEnumerable<string> formats)
    {
        var normalized = formats
            .Where(format => !string.IsNullOrWhiteSpace(format))
            .Select(format => format.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(format => format is "vtt" or "srt")
            .ToList();

        if (normalized.Count == 0)
        {
            throw new InvalidOperationException("At least one supported transcription output format is required.");
        }

        return normalized;
    }
}

public sealed class ListVideoTranscriptionsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public ListVideoTranscriptionsService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<IReadOnlyList<VideoTranscriptionDto>> Handle(Guid videoId, string? shareToken, CancellationToken cancellationToken)
    {
        await TranscriptionGuards.EnsureCanViewVideoAsync(
            videoId,
            _currentUserService,
            _authorizationService,
            shareToken,
            cancellationToken);

        var transcriptions = await _unitOfWork.VideoTranscriptions.GetByVideoIdAsync(videoId, cancellationToken);
        return transcriptions.Select(TranscriptionMapper.ToDto).ToArray();
    }
}

public sealed class GetVideoTranscriptionFileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStorageService _storageService;

    public GetVideoTranscriptionFileService(
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

    public async Task<StoredFileDescriptor> Handle(
        Guid videoId,
        Guid transcriptionId,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        await TranscriptionGuards.EnsureCanViewVideoAsync(
            videoId,
            _currentUserService,
            _authorizationService,
            shareToken,
            cancellationToken);

        var transcription = await _unitOfWork.VideoTranscriptions.GetByIdAsync(transcriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("VideoTranscription", transcriptionId);

        if (transcription.VideoId != videoId)
        {
            throw new InvalidOperationException("Transcription does not belong to the specified video.");
        }

        if (transcription.Status != TranscriptionStatus.Completed)
        {
            throw new InvalidOperationException("Transcription is not ready.");
        }

        var contentType = transcription.Format.Equals("VTT", StringComparison.OrdinalIgnoreCase)
            ? "text/vtt"
            : "application/x-subrip";

        return await _storageService.OpenReadAsync(transcription.StoragePath, contentType, cancellationToken);
    }
}

public sealed class CompleteVideoTranscriptionCallbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly ILogger<CompleteVideoTranscriptionCallbackService> _logger;

    public CompleteVideoTranscriptionCallbackService(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        ILogger<CompleteVideoTranscriptionCallbackService> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task Handle(TranscriptionCallbackRequestDto request, CancellationToken cancellationToken)
    {
        var activeRows = await _unitOfWork.VideoTranscriptions.GetByVideoAndStatusAsync(
            request.VideoId,
            TranscriptionStatus.Pending,
            TranscriptionStatus.Processing);

        if (activeRows.Count == 0)
        {
            _logger.LogWarning(
                "Received transcription callback for video {VideoId} with no active rows. Correlation={CorrelationId}, workerJobId={WorkerJobId}",
                request.VideoId,
                request.CorrelationId,
                request.WorkerJobId);
            return;
        }

        if (!string.Equals(request.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var row in activeRows)
            {
                row.Fail();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var detectedLanguage = string.IsNullOrWhiteSpace(request.Language)
            ? null
            : request.Language.Trim().ToLowerInvariant();

        foreach (var row in activeRows)
        {
            var expectedFileName = row.Format.Equals("VTT", StringComparison.OrdinalIgnoreCase)
                ? "captions.vtt"
                : "captions.srt";

            var artifact = request.Artifacts.FirstOrDefault(candidate =>
                candidate.Kind.Equals("local_path", StringComparison.OrdinalIgnoreCase) &&
                candidate.Path.EndsWith(expectedFileName, StringComparison.OrdinalIgnoreCase));

            if (artifact is null)
            {
                row.Fail();
                continue;
            }

            var storedPath = await _storageService.ImportFileAsync(artifact.Path, row.StoragePath, cancellationToken);
            row.Complete(storedPath, detectedLanguage);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public static class TranscriptionMapper
{
    public static VideoTranscriptionDto ToDto(VideoTranscription transcription)
    {
        return new VideoTranscriptionDto(
            transcription.Id,
            transcription.VideoId,
            transcription.Language,
            transcription.Format,
            transcription.Status.ToString(),
            transcription.Source,
            transcription.CreatedAt,
            transcription.UpdatedAt);
    }
}

internal static class TranscriptionGuards
{
    public static async Task EnsureCanViewVideoAsync(
        Guid videoId,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        var canView = await authorizationService.CanViewVideoAsync(
            videoId,
            currentUserService.UserId,
            currentUserService.Role,
            shareToken,
            cancellationToken);
        if (!canView)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video.");
        }
    }
}
