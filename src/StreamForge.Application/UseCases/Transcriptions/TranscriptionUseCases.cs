using Microsoft.Extensions.Logging;
using System.Text.Json;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Transcriptions;

public sealed class ReconcileTranscriptionOrphansService
{
    private static readonly TimeSpan PendingCorrelationGracePeriod = TimeSpan.FromMinutes(2);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly CompleteVideoTranscriptionCallbackService _completeVideoTranscriptionCallbackService;
    private readonly ILogger<ReconcileTranscriptionOrphansService> _logger;

    public ReconcileTranscriptionOrphansService(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider,
        CompleteVideoTranscriptionCallbackService completeVideoTranscriptionCallbackService,
        ILogger<ReconcileTranscriptionOrphansService> logger)
    {
        _unitOfWork = unitOfWork;
        _transcriptionProvider = transcriptionProvider;
        _completeVideoTranscriptionCallbackService = completeVideoTranscriptionCallbackService;
        _logger = logger;
    }

    public async Task<bool> ReconcileVideoAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var activeRows = await _unitOfWork.VideoTranscriptions.GetByVideoAndStatusAsync(
            videoId,
            TranscriptionStatus.Pending,
            TranscriptionStatus.Processing);

        if (activeRows.Count == 0)
        {
            return false;
        }

        return await ReconcileRowsAsync(activeRows, cancellationToken);
    }

    public async Task<bool> ReconcileRowsAsync(
        IReadOnlyCollection<VideoTranscription> rows,
        CancellationToken cancellationToken)
    {
        var activeRows = rows
            .Where(transcription => transcription.Status is TranscriptionStatus.Pending or TranscriptionStatus.Processing)
            .ToArray();
        if (activeRows.Length == 0)
        {
            return false;
        }

        var changed = false;

        foreach (var group in activeRows.GroupBy(GetGroupKey))
        {
            var ordered = group
                .OrderBy(transcription => transcription.CreatedAt)
                .ThenBy(transcription => transcription.Format)
                .ToArray();
            var primary = ordered[0];

            if (ShouldFailWithoutWorker(primary))
            {
                FailRows(
                    ordered,
                    primary.Status == TranscriptionStatus.Processing
                        ? "Transcription processing row is orphaned because it has no worker job id."
                        : "Transcription pending row is stale and has no correlation or worker job id.");
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(primary.WorkerJobId))
            {
                continue;
            }

            TranscriptionProviderJobStatus? liveStatus;
            try
            {
                liveStatus = await _transcriptionProvider.GetJobStatusAsync(primary.WorkerJobId, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Skipping orphan reconciliation for worker job {WorkerJobId} because live status lookup failed.",
                    primary.WorkerJobId);
                continue;
            }

            if (liveStatus is null)
            {
                FailRows(ordered, "Transcription worker job could not be found.");
                changed = true;
                continue;
            }

            if (string.Equals(liveStatus.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                FailRows(ordered, liveStatus.Message ?? "Transcription worker reported failure.");
                changed = true;
                continue;
            }

            if (string.Equals(liveStatus.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _transcriptionProvider.GetJobResultAsync(primary.WorkerJobId, cancellationToken);
                if (result is null)
                {
                    continue;
                }

                await _completeVideoTranscriptionCallbackService.Handle(
                    new TranscriptionCallbackRequestDto(
                        primary.CorrelationId ?? liveStatus.CorrelationId,
                        primary.VideoId,
                        primary.WorkerJobId,
                        "completed",
                        result.Language,
                        result.Artifacts.Select(artifact => new TranscriptionCallbackArtifactDto(artifact.Kind, artifact.Path)).ToArray(),
                        null,
                        primary.Source,
                        primary.Model),
                    cancellationToken);
                changed = true;
            }
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }

    private static string GetGroupKey(VideoTranscription transcription)
    {
        var token = !string.IsNullOrWhiteSpace(transcription.WorkerJobId)
            ? $"worker:{transcription.WorkerJobId}"
            : !string.IsNullOrWhiteSpace(transcription.CorrelationId)
                ? $"correlation:{transcription.CorrelationId}"
                : $"row:{transcription.Id:N}";

        return $"{token}|lang:{transcription.Language}";
    }

    private static bool ShouldFailWithoutWorker(VideoTranscription transcription)
    {
        if (transcription.Status == TranscriptionStatus.Processing &&
            string.IsNullOrWhiteSpace(transcription.WorkerJobId))
        {
            return true;
        }

        if (transcription.Status == TranscriptionStatus.Pending &&
            string.IsNullOrWhiteSpace(transcription.WorkerJobId) &&
            string.IsNullOrWhiteSpace(transcription.CorrelationId))
        {
            var updatedAt = transcription.UpdatedAt ?? transcription.CreatedAt;
            return DateTime.UtcNow - updatedAt > PendingCorrelationGracePeriod;
        }

        return false;
    }

    private static void FailRows(IEnumerable<VideoTranscription> rows, string reason)
    {
        foreach (var row in rows.Where(transcription => transcription.Status is TranscriptionStatus.Pending or TranscriptionStatus.Processing))
        {
            row.Fail(reason);
        }
    }
}

public sealed class StartVideoTranscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly TranscriptionOptions _options;
    private readonly ResolveTranscriptionSettingsService _resolveTranscriptionSettings;
    private readonly ReconcileTranscriptionOrphansService _reconcileTranscriptionOrphans;
    private readonly ILogger<StartVideoTranscriptionService> _logger;

    public StartVideoTranscriptionService(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider,
        TranscriptionOptions options,
        ResolveTranscriptionSettingsService resolveTranscriptionSettings,
        ReconcileTranscriptionOrphansService reconcileTranscriptionOrphans,
        ILogger<StartVideoTranscriptionService> logger)
    {
        _unitOfWork = unitOfWork;
        _transcriptionProvider = transcriptionProvider;
        _options = options;
        _resolveTranscriptionSettings = resolveTranscriptionSettings;
        _reconcileTranscriptionOrphans = reconcileTranscriptionOrphans;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VideoTranscriptionDto>> Handle(
        Guid videoId,
        string? language,
        IReadOnlyCollection<string>? outputFormats,
        CancellationToken cancellationToken = default)
    {
        var effectiveSettings = await _resolveTranscriptionSettings.Handle(cancellationToken);

        if (!effectiveSettings.Enabled)
        {
            throw new InvalidOperationException("Transcription is disabled.");
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video must be ready before transcription can start.");
        }

        await _reconcileTranscriptionOrphans.ReconcileVideoAsync(videoId, cancellationToken);

        var activeRecords = await _unitOfWork.VideoTranscriptions.GetByVideoAndStatusAsync(
            videoId,
            TranscriptionStatus.Pending,
            TranscriptionStatus.Processing);
        if (activeRecords.Count > 0)
        {
            return activeRecords
                .Select(transcription => TranscriptionMapper.ToDto(transcription, TranscriptionMapper.EmptyLiveStatuses))
                .ToArray();
        }

        var originalFile = await _unitOfWork.VideoFiles.GetOriginalByVideoIdAsync(videoId, cancellationToken)
            ?? throw new InvalidOperationException($"Video {videoId} does not have an original source file.");

        var requestedLanguage = NormalizeLanguage(language) ?? NormalizeLanguage(effectiveSettings.DefaultLanguage) ?? "auto";
        var normalizedOutputFormats = NormalizeOutputFormats(outputFormats ?? effectiveSettings.OutputFormats);
        var transcriptions = new List<VideoTranscription>(normalizedOutputFormats.Count);
        var correlationId = $"{videoId:N}:{requestedLanguage}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        foreach (var format in normalizedOutputFormats)
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
                    effectiveSettings.Provider);
                await _unitOfWork.VideoTranscriptions.AddAsync(existing, cancellationToken);
            }

            existing.QueueForProcessing(destinationStoragePath, effectiveSettings.Provider, correlationId, effectiveSettings.Model);

            transcriptions.Add(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var callbackUrl = $"{_options.CallbackBaseUrl.TrimEnd('/')}/internal/transcriptions/callback";

        try
        {
            var result = await _transcriptionProvider.SubmitAsync(
                new TranscriptionProviderRequest(
                    videoId,
                    correlationId,
                    "local_path",
                    originalFile.FilePath,
                    requestedLanguage == "auto" ? null : requestedLanguage,
                    normalizedOutputFormats.Select(format => format.ToLowerInvariant()).ToArray(),
                    callbackUrl,
                    _options.WorkerCallbackSecret,
                    effectiveSettings.Model,
                    effectiveSettings.Device,
                    effectiveSettings.ComputeType,
                    effectiveSettings.BeamSize,
                    effectiveSettings.EnableVad,
                    effectiveSettings.EnableWordTimestamps),
                cancellationToken);

            foreach (var transcription in transcriptions)
            {
                transcription.StartProcessing(result.JobId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transcription submitted for video {VideoId}. Correlation={CorrelationId}, workerJobId={WorkerJobId}, formats={Formats}",
                videoId,
                correlationId,
                result.JobId,
                string.Join(", ", normalizedOutputFormats));

            return transcriptions.Select(transcription => TranscriptionMapper.ToDto(transcription, TranscriptionMapper.EmptyLiveStatuses)).ToArray();
        }
        catch (Exception exception)
        {
            foreach (var transcription in transcriptions)
            {
                transcription.Fail(exception.Message);
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
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly ReconcileTranscriptionOrphansService _reconcileTranscriptionOrphans;

    public ListVideoTranscriptionsService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ITranscriptionProvider transcriptionProvider,
        ReconcileTranscriptionOrphansService reconcileTranscriptionOrphans)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _transcriptionProvider = transcriptionProvider;
        _reconcileTranscriptionOrphans = reconcileTranscriptionOrphans;
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
        await _reconcileTranscriptionOrphans.ReconcileRowsAsync(transcriptions, cancellationToken);
        var liveStatuses = await TranscriptionMapper.GetLiveStatusesAsync(transcriptions, _transcriptionProvider, cancellationToken);
        return transcriptions.Select(transcription => TranscriptionMapper.ToDto(transcription, liveStatuses)).ToArray();
    }
}

public sealed class GetVideoTranscriptionStatusService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly ReconcileTranscriptionOrphansService _reconcileTranscriptionOrphans;

    public GetVideoTranscriptionStatusService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ITranscriptionProvider transcriptionProvider,
        ReconcileTranscriptionOrphansService reconcileTranscriptionOrphans)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _transcriptionProvider = transcriptionProvider;
        _reconcileTranscriptionOrphans = reconcileTranscriptionOrphans;
    }

    public async Task<VideoTranscriptionDto> Handle(
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

        await _reconcileTranscriptionOrphans.ReconcileRowsAsync([transcription], cancellationToken);
        var liveStatuses = await TranscriptionMapper.GetLiveStatusesAsync([transcription], _transcriptionProvider, cancellationToken);
        return TranscriptionMapper.ToDto(transcription, liveStatuses);
    }
}

public sealed class ListVideoTranscriptionJobsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly ReconcileTranscriptionOrphansService _reconcileTranscriptionOrphans;

    public ListVideoTranscriptionJobsService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ITranscriptionProvider transcriptionProvider,
        ReconcileTranscriptionOrphansService reconcileTranscriptionOrphans)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _transcriptionProvider = transcriptionProvider;
        _reconcileTranscriptionOrphans = reconcileTranscriptionOrphans;
    }

    public async Task<IReadOnlyList<VideoTranscriptionJobDto>> Handle(
        Guid videoId,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        await TranscriptionGuards.EnsureCanViewVideoAsync(
            videoId,
            _currentUserService,
            _authorizationService,
            shareToken,
            cancellationToken);

        var transcriptions = await _unitOfWork.VideoTranscriptions.GetByVideoIdAsync(videoId, cancellationToken);
        await _reconcileTranscriptionOrphans.ReconcileRowsAsync(transcriptions, cancellationToken);
        var liveStatuses = await TranscriptionMapper.GetLiveStatusesAsync(transcriptions, _transcriptionProvider, cancellationToken);
        return TranscriptionMapper.ToJobDtos(transcriptions, liveStatuses);
    }
}

public sealed class ListAdminTranscriptionJobsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly ReconcileTranscriptionOrphansService _reconcileTranscriptionOrphans;

    public ListAdminTranscriptionJobsService(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider,
        ReconcileTranscriptionOrphansService reconcileTranscriptionOrphans)
    {
        _unitOfWork = unitOfWork;
        _transcriptionProvider = transcriptionProvider;
        _reconcileTranscriptionOrphans = reconcileTranscriptionOrphans;
    }

    public async Task<IReadOnlyList<VideoTranscriptionJobDto>> Handle(
        string? status,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<VideoTranscription> transcriptions;

        if (string.IsNullOrWhiteSpace(status))
        {
            transcriptions = await _unitOfWork.VideoTranscriptions.GetAllOrderedAsync(cancellationToken);
        }
        else if (Enum.TryParse<TranscriptionStatus>(status.Trim(), true, out var parsedStatus))
        {
            transcriptions = await _unitOfWork.VideoTranscriptions.GetByStatusesAsync(cancellationToken, parsedStatus);
        }
        else
        {
            throw new InvalidOperationException("Unsupported transcription status filter.");
        }

        await _reconcileTranscriptionOrphans.ReconcileRowsAsync(transcriptions, cancellationToken);
        var liveStatuses = await TranscriptionMapper.GetLiveStatusesAsync(transcriptions, _transcriptionProvider, cancellationToken);
        return TranscriptionMapper.ToJobDtos(transcriptions, liveStatuses);
    }
}

public sealed class GetAdminTranscriptionJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly ReconcileTranscriptionOrphansService _reconcileTranscriptionOrphans;

    public GetAdminTranscriptionJobService(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider,
        ReconcileTranscriptionOrphansService reconcileTranscriptionOrphans)
    {
        _unitOfWork = unitOfWork;
        _transcriptionProvider = transcriptionProvider;
        _reconcileTranscriptionOrphans = reconcileTranscriptionOrphans;
    }

    public async Task<VideoTranscriptionJobDto> Handle(
        string jobKey,
        CancellationToken cancellationToken)
    {
        var transcriptions = await TranscriptionAdminJobResolver.ResolveRowsAsync(_unitOfWork, jobKey, cancellationToken);
        if (transcriptions.Count == 0)
        {
            throw new EntityNotFoundException("TranscriptionJob", jobKey);
        }

        await _reconcileTranscriptionOrphans.ReconcileRowsAsync(transcriptions, cancellationToken);
        var liveStatuses = await TranscriptionMapper.GetLiveStatusesAsync(transcriptions, _transcriptionProvider, cancellationToken);
        return TranscriptionMapper.ToJobDtos(transcriptions, liveStatuses).Single();
    }
}

public sealed class RetryAdminTranscriptionJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly StartVideoTranscriptionService _startVideoTranscriptionService;
    private readonly ITranscriptionProvider _transcriptionProvider;

    public RetryAdminTranscriptionJobService(
        IUnitOfWork unitOfWork,
        StartVideoTranscriptionService startVideoTranscriptionService,
        ITranscriptionProvider transcriptionProvider)
    {
        _unitOfWork = unitOfWork;
        _startVideoTranscriptionService = startVideoTranscriptionService;
        _transcriptionProvider = transcriptionProvider;
    }

    public async Task<VideoTranscriptionJobDto> Handle(
        string jobKey,
        CancellationToken cancellationToken)
    {
        var transcriptions = await TranscriptionAdminJobResolver.ResolveRowsAsync(_unitOfWork, jobKey, cancellationToken);
        if (transcriptions.Count == 0)
        {
            throw new EntityNotFoundException("TranscriptionJob", jobKey);
        }

        if (transcriptions.Any(transcription => transcription.Status is TranscriptionStatus.Pending or TranscriptionStatus.Processing))
        {
            throw new InvalidOperationException("Cannot retry a transcription job that is already pending or processing.");
        }

        var primary = transcriptions.OrderBy(transcription => transcription.CreatedAt).First();
        var outputFormats = transcriptions.Select(transcription => transcription.Format).ToArray();

        await _startVideoTranscriptionService.Handle(
            primary.VideoId,
            primary.Language,
            outputFormats,
            cancellationToken);

        var refreshed = await TranscriptionAdminJobResolver.ResolveLatestRowsForVideoLanguageAsync(
            _unitOfWork,
            primary.VideoId,
            primary.Language,
            outputFormats,
            cancellationToken);

        var liveStatuses = await TranscriptionMapper.GetLiveStatusesAsync(refreshed, _transcriptionProvider, cancellationToken);
        return TranscriptionMapper.ToJobDtos(refreshed, liveStatuses).Single();
    }
}

public sealed class ResyncAdminTranscriptionJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptionProvider _transcriptionProvider;
    private readonly CompleteVideoTranscriptionCallbackService _completeVideoTranscriptionCallbackService;

    public ResyncAdminTranscriptionJobService(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider,
        CompleteVideoTranscriptionCallbackService completeVideoTranscriptionCallbackService)
    {
        _unitOfWork = unitOfWork;
        _transcriptionProvider = transcriptionProvider;
        _completeVideoTranscriptionCallbackService = completeVideoTranscriptionCallbackService;
    }

    public async Task<VideoTranscriptionJobDto> Handle(
        string jobKey,
        CancellationToken cancellationToken)
    {
        var transcriptions = await TranscriptionAdminJobResolver.ResolveRowsAsync(_unitOfWork, jobKey, cancellationToken);
        if (transcriptions.Count == 0)
        {
            throw new EntityNotFoundException("TranscriptionJob", jobKey);
        }

        var primary = transcriptions.OrderBy(transcription => transcription.CreatedAt).First();
        if (string.IsNullOrWhiteSpace(primary.WorkerJobId))
        {
            var emptyStatuses = TranscriptionMapper.EmptyLiveStatuses;
            return TranscriptionMapper.ToJobDtos(transcriptions, emptyStatuses).Single();
        }

        var liveStatus = await _transcriptionProvider.GetJobStatusAsync(primary.WorkerJobId, cancellationToken);
        if (liveStatus is null)
        {
            var emptyStatuses = TranscriptionMapper.EmptyLiveStatuses;
            return TranscriptionMapper.ToJobDtos(transcriptions, emptyStatuses).Single();
        }

        if (string.Equals(liveStatus.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _transcriptionProvider.GetJobResultAsync(primary.WorkerJobId, cancellationToken);
            if (result is not null)
            {
                await _completeVideoTranscriptionCallbackService.Handle(
                    new TranscriptionCallbackRequestDto(
                        primary.CorrelationId ?? liveStatus.CorrelationId,
                        primary.VideoId,
                        primary.WorkerJobId,
                        "completed",
                        result.Language,
                        result.Artifacts.Select(artifact => new TranscriptionCallbackArtifactDto(artifact.Kind, artifact.Path)).ToArray(),
                        null,
                        primary.Source,
                        primary.Model),
                    cancellationToken);
            }
        }
        else if (string.Equals(liveStatus.Status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var transcription in transcriptions.Where(transcription => transcription.Status is TranscriptionStatus.Pending or TranscriptionStatus.Processing))
            {
                transcription.Fail(liveStatus.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var refreshed = await TranscriptionAdminJobResolver.ResolveRowsAsync(_unitOfWork, jobKey, cancellationToken);
        var refreshedStatuses = await TranscriptionMapper.GetLiveStatusesAsync(refreshed, _transcriptionProvider, cancellationToken);
        return TranscriptionMapper.ToJobDtos(refreshed, refreshedStatuses).Single();
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
        IReadOnlyList<VideoTranscription> activeRows = [];

        if (!string.IsNullOrWhiteSpace(request.WorkerJobId))
        {
            activeRows = await _unitOfWork.VideoTranscriptions.GetByWorkerJobIdAsync(request.WorkerJobId, cancellationToken);
        }

        if (activeRows.Count == 0 && !string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            activeRows = await _unitOfWork.VideoTranscriptions.GetByCorrelationIdAsync(request.CorrelationId, cancellationToken);
        }

        if (activeRows.Count == 0)
        {
            activeRows = await _unitOfWork.VideoTranscriptions.GetByVideoAndStatusAsync(
                request.VideoId,
                TranscriptionStatus.Pending,
                TranscriptionStatus.Processing);
        }

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
                row.Fail(request.FailureReason);
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
                row.Fail("Expected caption artifact was not returned by the transcription worker callback.");
                continue;
            }

            var storedPath = await _storageService.ImportFileAsync(artifact.Path, row.StoragePath, cancellationToken);
            row.Complete(storedPath, detectedLanguage, request.Provider, request.Model);
        }

        await PersistTranscriptChunksAsync(activeRows, request, detectedLanguage, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistTranscriptChunksAsync(
        IReadOnlyList<VideoTranscription> activeRows,
        TranscriptionCallbackRequestDto request,
        string? detectedLanguage,
        CancellationToken cancellationToken)
    {
        var segmentsArtifact = request.Artifacts.FirstOrDefault(candidate =>
            candidate.Kind.Equals("local_path", StringComparison.OrdinalIgnoreCase) &&
            candidate.Path.EndsWith("segments.json", StringComparison.OrdinalIgnoreCase));

        if (segmentsArtifact is null)
        {
            return;
        }

        if (!File.Exists(segmentsArtifact.Path))
        {
            _logger.LogWarning(
                "Transcription callback referenced missing segments artifact for video {VideoId}: {ArtifactPath}",
                request.VideoId,
                segmentsArtifact.Path);
            return;
        }

        var primaryRow = activeRows
            .OrderBy(row => row.Format.Equals("VTT", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(row => row.CreatedAt)
            .First();

        var language = detectedLanguage ?? primaryRow.Language;
        var json = await File.ReadAllTextAsync(segmentsArtifact.Path, cancellationToken);
        var segments = JsonSerializer.Deserialize<TranscriptionSegmentArtifactRecord[]>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];

        await _unitOfWork.VideoTranscriptChunks.DeleteByVideoAndLanguageAsync(primaryRow.VideoId, language, cancellationToken);

        foreach (var segment in segments.Where(segment => !string.IsNullOrWhiteSpace(segment.Text)))
        {
            var chunk = VideoTranscriptChunk.Create(
                primaryRow.VideoId,
                primaryRow.Id,
                language,
                segment.StartSeconds,
                segment.EndSeconds,
                segment.Text);

            await _unitOfWork.VideoTranscriptChunks.AddAsync(chunk, cancellationToken);
        }
    }

    private sealed record TranscriptionSegmentArtifactRecord(
        double StartSeconds,
        double EndSeconds,
        string Text);
}

public static class TranscriptionMapper
{
    public static readonly IReadOnlyDictionary<string, TranscriptionProviderJobStatus> EmptyLiveStatuses =
        new Dictionary<string, TranscriptionProviderJobStatus>(StringComparer.Ordinal);

    public static VideoTranscriptionDto ToDto(
        VideoTranscription transcription,
        IReadOnlyDictionary<string, TranscriptionProviderJobStatus> liveStatuses)
    {
        var liveStatus = ResolveLiveStatus(transcription, liveStatuses);
        return new VideoTranscriptionDto(
            transcription.Id,
            transcription.VideoId,
            transcription.Language,
            transcription.Format,
            transcription.Status.ToString(),
            transcription.Source,
            transcription.CorrelationId,
            transcription.WorkerJobId,
            transcription.Model,
            transcription.FailureReason,
            transcription.CreatedAt,
            transcription.UpdatedAt,
            liveStatus);
    }

    public static async Task<IReadOnlyDictionary<string, TranscriptionProviderJobStatus>> GetLiveStatusesAsync(
        IReadOnlyCollection<VideoTranscription> transcriptions,
        ITranscriptionProvider transcriptionProvider,
        CancellationToken cancellationToken)
    {
        var workerJobIds = transcriptions
            .Where(transcription =>
                !string.IsNullOrWhiteSpace(transcription.WorkerJobId) &&
                transcription.Status is TranscriptionStatus.Pending or TranscriptionStatus.Processing)
            .Select(transcription => transcription.WorkerJobId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (workerJobIds.Length == 0)
        {
            return new Dictionary<string, TranscriptionProviderJobStatus>(StringComparer.Ordinal);
        }

        var statuses = new Dictionary<string, TranscriptionProviderJobStatus>(StringComparer.Ordinal);

        foreach (var workerJobId in workerJobIds)
        {
            try
            {
                var status = await transcriptionProvider.GetJobStatusAsync(workerJobId, cancellationToken);
                if (status is not null)
                {
                    statuses[workerJobId] = status;
                }
            }
            catch
            {
                // Persisted row state remains the fallback when live worker status is unavailable.
            }
        }

        return statuses;
    }

    public static IReadOnlyList<VideoTranscriptionJobDto> ToJobDtos(
        IReadOnlyCollection<VideoTranscription> transcriptions,
        IReadOnlyDictionary<string, TranscriptionProviderJobStatus> liveStatuses)
    {
        return transcriptions
            .GroupBy(GetJobGroupKey)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(transcription => transcription.CreatedAt)
                    .ThenBy(transcription => transcription.Format)
                    .ToArray();

                var primary = ordered[0];
                var liveStatus = ResolveLiveStatus(primary, liveStatuses);

                return new VideoTranscriptionJobDto(
                    GetClientJobKey(primary),
                    primary.VideoId,
                    primary.Language,
                    ResolveJobStatus(ordered, liveStatus),
                    primary.Source,
                    primary.CorrelationId,
                    primary.WorkerJobId,
                    primary.Model,
                    ResolveFailureReason(ordered),
                    ordered.Min(transcription => transcription.CreatedAt),
                    ordered.Max(transcription => transcription.UpdatedAt),
                    liveStatus,
                    ordered
                        .Select(transcription => new VideoTranscriptionArtifactDto(
                            transcription.Id,
                            transcription.Format,
                            transcription.Status.ToString(),
                            transcription.FailureReason,
                            transcription.CreatedAt,
                            transcription.UpdatedAt))
                        .ToArray());
            })
            .OrderByDescending(job => job.CreatedAt)
            .ToArray();
    }

    private static string GetJobGroupKey(VideoTranscription transcription)
    {
        var token = !string.IsNullOrWhiteSpace(transcription.WorkerJobId)
            ? $"worker:{transcription.WorkerJobId}"
            : !string.IsNullOrWhiteSpace(transcription.CorrelationId)
                ? $"correlation:{transcription.CorrelationId}"
                : $"row:{transcription.Id:N}";

        return $"{token}|lang:{transcription.Language}";
    }

    private static string GetClientJobKey(VideoTranscription transcription)
    {
        if (!string.IsNullOrWhiteSpace(transcription.WorkerJobId))
        {
            return transcription.WorkerJobId!;
        }

        if (!string.IsNullOrWhiteSpace(transcription.CorrelationId))
        {
            return transcription.CorrelationId!;
        }

        return transcription.Id.ToString("N");
    }

    private static string ResolveJobStatus(
        IReadOnlyCollection<VideoTranscription> transcriptions,
        TranscriptionLiveStatusDto? liveStatus)
    {
        if (liveStatus is not null)
        {
            return liveStatus.Status;
        }

        var statuses = transcriptions.Select(transcription => transcription.Status).Distinct().ToArray();
        if (statuses.Length == 1)
        {
            return statuses[0].ToString();
        }

        if (statuses.Contains(TranscriptionStatus.Processing))
        {
            return TranscriptionStatus.Processing.ToString();
        }

        if (statuses.Contains(TranscriptionStatus.Pending))
        {
            return TranscriptionStatus.Pending.ToString();
        }

        if (statuses.All(status => status == TranscriptionStatus.Completed))
        {
            return TranscriptionStatus.Completed.ToString();
        }

        if (statuses.All(status => status == TranscriptionStatus.Failed))
        {
            return TranscriptionStatus.Failed.ToString();
        }

        return "Partial";
    }

    private static string? ResolveFailureReason(IReadOnlyCollection<VideoTranscription> transcriptions)
    {
        return transcriptions
            .Select(transcription => transcription.FailureReason)
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));
    }

    private static TranscriptionLiveStatusDto? ResolveLiveStatus(
        VideoTranscription transcription,
        IReadOnlyDictionary<string, TranscriptionProviderJobStatus> liveStatuses)
    {
        if (string.IsNullOrWhiteSpace(transcription.WorkerJobId) ||
            !liveStatuses.TryGetValue(transcription.WorkerJobId, out var liveStatus))
        {
            return null;
        }

        return new TranscriptionLiveStatusDto(
            liveStatus.Status,
            liveStatus.ProgressPercent,
            liveStatus.Stage,
            liveStatus.Message,
            liveStatus.Language,
            liveStatus.StartedAt,
            liveStatus.CompletedAt,
            liveStatus.MediaDurationSeconds,
            liveStatus.TranscribedUntilSeconds);
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

internal static class TranscriptionAdminJobResolver
{
    public static async Task<IReadOnlyList<VideoTranscription>> ResolveRowsAsync(
        IUnitOfWork unitOfWork,
        string jobKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
        {
            return [];
        }

        var byWorkerJobId = await unitOfWork.VideoTranscriptions.GetByWorkerJobIdAsync(jobKey, cancellationToken);
        if (byWorkerJobId.Count > 0)
        {
            return byWorkerJobId;
        }

        var byCorrelationId = await unitOfWork.VideoTranscriptions.GetByCorrelationIdAsync(jobKey, cancellationToken);
        if (byCorrelationId.Count > 0)
        {
            return byCorrelationId;
        }

        if (Guid.TryParse(jobKey, out var rowId))
        {
            var row = await unitOfWork.VideoTranscriptions.GetByIdAsync(rowId, cancellationToken);
            if (row is not null)
            {
                return [row];
            }
        }

        return [];
    }

    public static async Task<IReadOnlyList<VideoTranscription>> ResolveLatestRowsForVideoLanguageAsync(
        IUnitOfWork unitOfWork,
        Guid videoId,
        string language,
        IReadOnlyCollection<string> formats,
        CancellationToken cancellationToken)
    {
        var rows = new List<VideoTranscription>(formats.Count);

        foreach (var format in formats)
        {
            var row = await unitOfWork.VideoTranscriptions.GetByVideoLanguageAndFormatAsync(
                videoId,
                language,
                format,
                cancellationToken);

            if (row is not null)
            {
                rows.Add(row);
            }
        }

        if (rows.Count == 0)
        {
            return [];
        }

        if (!string.IsNullOrWhiteSpace(rows[0].WorkerJobId))
        {
            return await unitOfWork.VideoTranscriptions.GetByWorkerJobIdAsync(rows[0].WorkerJobId!, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(rows[0].CorrelationId))
        {
            return await unitOfWork.VideoTranscriptions.GetByCorrelationIdAsync(rows[0].CorrelationId!, cancellationToken);
        }

        return rows;
    }
}
