using System.Text.Json;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Transcriptions;

internal static class TranscriptionSettingKeys
{
    public const string Enabled = "transcription.enabled";
    public const string AutoTranscribeOnReady = "transcription.autoTranscribeOnReady";
    public const string Provider = "transcription.provider";
    public const string DefaultLanguage = "transcription.defaultLanguage";
    public const string OutputFormats = "transcription.outputFormats";
    public const string LocalModel = "transcription.localFasterWhisper.model";
    public const string LocalDevice = "transcription.localFasterWhisper.device";
    public const string LocalComputeType = "transcription.localFasterWhisper.computeType";
    public const string LocalBeamSize = "transcription.localFasterWhisper.beamSize";
    public const string LocalEnableVad = "transcription.localFasterWhisper.enableVad";
    public const string LocalEnableWordTimestamps = "transcription.localFasterWhisper.enableWordTimestamps";

    public static readonly string[] All =
    [
        Enabled,
        AutoTranscribeOnReady,
        Provider,
        DefaultLanguage,
        OutputFormats,
        LocalModel,
        LocalDevice,
        LocalComputeType,
        LocalBeamSize,
        LocalEnableVad,
        LocalEnableWordTimestamps
    ];
}

public sealed record EffectiveTranscriptionSettings(
    bool Enabled,
    bool AutoTranscribeOnReady,
    string Provider,
    string? DefaultLanguage,
    string[] OutputFormats,
    string Model,
    string Device,
    string ComputeType,
    int BeamSize,
    bool EnableVad,
    bool EnableWordTimestamps);

public sealed class ResolveTranscriptionSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TranscriptionOptions _defaults;

    public ResolveTranscriptionSettingsService(IUnitOfWork unitOfWork, TranscriptionOptions defaults)
    {
        _unitOfWork = unitOfWork;
        _defaults = defaults;
    }

    public async Task<EffectiveTranscriptionSettings> Handle(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.SystemSettings.GetByKeysAsync(TranscriptionSettingKeys.All, cancellationToken);
        var map = settings.ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal);

        return new EffectiveTranscriptionSettings(
            GetBool(map, TranscriptionSettingKeys.Enabled, _defaults.Enabled),
            GetBool(map, TranscriptionSettingKeys.AutoTranscribeOnReady, _defaults.AutoTranscribeOnReady),
            GetString(map, TranscriptionSettingKeys.Provider, _defaults.Provider),
            GetNullableString(map, TranscriptionSettingKeys.DefaultLanguage, _defaults.DefaultLanguage),
            GetFormats(map, _defaults.OutputFormats),
            GetString(map, TranscriptionSettingKeys.LocalModel, _defaults.LocalFasterWhisper.Model),
            GetString(map, TranscriptionSettingKeys.LocalDevice, _defaults.LocalFasterWhisper.Device),
            GetString(map, TranscriptionSettingKeys.LocalComputeType, _defaults.LocalFasterWhisper.ComputeType),
            GetInt(map, TranscriptionSettingKeys.LocalBeamSize, _defaults.LocalFasterWhisper.BeamSize),
            GetBool(map, TranscriptionSettingKeys.LocalEnableVad, _defaults.LocalFasterWhisper.EnableVad),
            GetBool(map, TranscriptionSettingKeys.LocalEnableWordTimestamps, _defaults.LocalFasterWhisper.EnableWordTimestamps));
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> map, string key, bool fallback) =>
        map.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static int GetInt(IReadOnlyDictionary<string, string> map, string key, int fallback) =>
        map.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static string GetString(IReadOnlyDictionary<string, string> map, string key, string fallback) =>
        map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;

    private static string? GetNullableString(IReadOnlyDictionary<string, string> map, string key, string? fallback)
    {
        if (!map.TryGetValue(key, out var value))
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string[] GetFormats(IReadOnlyDictionary<string, string> map, string[] fallback)
    {
        if (!map.TryGetValue(TranscriptionSettingKeys.OutputFormats, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(raw);
            return parsed is { Length: > 0 } ? parsed : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}

public sealed class GetAdminTranscriptionSettingsService
{
    private readonly ResolveTranscriptionSettingsService _resolveSettings;

    public GetAdminTranscriptionSettingsService(ResolveTranscriptionSettingsService resolveSettings)
    {
        _resolveSettings = resolveSettings;
    }

    public async Task<AdminTranscriptionSettingsDto> Handle(CancellationToken cancellationToken = default)
    {
        var settings = await _resolveSettings.Handle(cancellationToken);
        return TranscriptionSettingsMapper.ToAdminDto(settings);
    }
}

public sealed class UpdateAdminTranscriptionSettingsService
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAdminTranscriptionSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminTranscriptionSettingsDto> Handle(
        UpdateAdminTranscriptionSettingsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            throw new ArgumentException("Provider is required.", nameof(request.Provider));
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("Model is required.", nameof(request.Model));
        }

        var normalizedFormats = request.OutputFormats
            .Where(format => !string.IsNullOrWhiteSpace(format))
            .Select(format => format.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedFormats.Length == 0)
        {
            throw new ArgumentException("At least one output format is required.", nameof(request.OutputFormats));
        }

        await UpsertSettingAsync(TranscriptionSettingKeys.Enabled, request.Enabled.ToString(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.AutoTranscribeOnReady, request.AutoTranscribeOnReady.ToString(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.Provider, request.Provider.Trim(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.DefaultLanguage, request.DefaultLanguage?.Trim(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.OutputFormats, JsonSerializer.Serialize(normalizedFormats), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.LocalModel, request.Model.Trim(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.LocalDevice, request.Device.Trim(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.LocalComputeType, request.ComputeType.Trim(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.LocalBeamSize, request.BeamSize.ToString(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.LocalEnableVad, request.EnableVad.ToString(), cancellationToken);
        await UpsertSettingAsync(TranscriptionSettingKeys.LocalEnableWordTimestamps, request.EnableWordTimestamps.ToString(), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminTranscriptionSettingsDto(
            request.Enabled,
            request.AutoTranscribeOnReady,
            request.Provider.Trim(),
            string.IsNullOrWhiteSpace(request.DefaultLanguage) ? null : request.DefaultLanguage.Trim().ToLowerInvariant(),
            normalizedFormats,
            request.Model.Trim(),
            request.Device.Trim(),
            request.ComputeType.Trim(),
            request.BeamSize,
            request.EnableVad,
            request.EnableWordTimestamps);
    }

    private async Task UpsertSettingAsync(string key, string? value, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.SystemSettings.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            await _unitOfWork.SystemSettings.AddAsync(SystemSetting.Create(key, value ?? string.Empty), cancellationToken);
            return;
        }

        existing.SetValue(value);
        await _unitOfWork.SystemSettings.UpdateAsync(existing, cancellationToken);
    }
}

public sealed class SearchVideoTranscriptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public SearchVideoTranscriptService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<PagedResponseDto<TranscriptSearchResultDto>> Handle(
        Guid videoId,
        string query,
        string? language,
        int page,
        int pageSize,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        await TranscriptionGuards.EnsureCanViewVideoAsync(
            videoId,
            _currentUserService,
            _authorizationService,
            shareToken,
            cancellationToken);

        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var result = await _unitOfWork.VideoTranscriptChunks.SearchKeywordAsync(
            videoId,
            query,
            language,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        return new PagedResponseDto<TranscriptSearchResultDto>(
            result.Items.Select(chunk => new TranscriptSearchResultDto(
                chunk.Id,
                chunk.VideoId,
                chunk.TranscriptionId,
                chunk.Language,
                chunk.StartSeconds,
                chunk.EndSeconds,
                chunk.Content))
            .ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.PageSize <= 0 ? 0 : (int)Math.Ceiling((double)result.TotalCount / result.PageSize),
            result.Page * result.PageSize < result.TotalCount,
            result.Page > 1);
    }
}

public sealed class GetVideoTranscriptionChunksService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public GetVideoTranscriptionChunksService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<IReadOnlyList<TranscriptChunkDto>> Handle(
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
            ?? throw new InvalidOperationException("Transcription was not found.");

        if (transcription.VideoId != videoId)
        {
            throw new InvalidOperationException("Transcription does not belong to the specified video.");
        }

        var chunks = await _unitOfWork.VideoTranscriptChunks.GetByTranscriptionIdAsync(transcriptionId, cancellationToken);
        if (chunks.Count == 0)
        {
            // Chunks are currently persisted once per video-language transcript set,
            // so sibling artifacts such as SRT should still resolve the shared rows.
            chunks = await _unitOfWork.VideoTranscriptChunks.GetByVideoAndLanguageAsync(
                videoId,
                transcription.Language,
                cancellationToken);
        }

        return chunks.Select(chunk => new TranscriptChunkDto(
                chunk.Id,
                chunk.VideoId,
                chunk.TranscriptionId,
                chunk.Language,
                chunk.StartSeconds,
                chunk.EndSeconds,
                chunk.Content))
            .ToArray();
    }
}

internal static class TranscriptionSettingsMapper
{
    public static AdminTranscriptionSettingsDto ToAdminDto(EffectiveTranscriptionSettings settings) =>
        new(
            settings.Enabled,
            settings.AutoTranscribeOnReady,
            settings.Provider,
            settings.DefaultLanguage,
            settings.OutputFormats,
            settings.Model,
            settings.Device,
            settings.ComputeType,
            settings.BeamSize,
            settings.EnableVad,
            settings.EnableWordTimestamps);
}
