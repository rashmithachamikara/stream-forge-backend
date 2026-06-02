namespace StreamForge.Application.Interfaces;

public sealed record MediaProbeResult(
    int DurationSeconds,
    int Width,
    int Height,
    int? Bitrate,
    string? Codec);

public sealed record HlsVariantResult(
    string Resolution,
    int Width,
    int Height,
    string PlaylistPath,
    long PlaylistSize,
    int? Bitrate,
    string? Codec);

public sealed record HlsOutputResult(
    string MasterPlaylistPath,
    long MasterPlaylistSize,
    IReadOnlyCollection<HlsVariantResult> Variants);

public sealed record ThumbnailOutputResult(
    string StoragePath,
    int Width,
    int Height,
    long SizeBytes,
    int TimestampSeconds);

public interface IMediaProcessingService
{
    Task<MediaProbeResult> ProbeAsync(string sourceStoragePath, CancellationToken cancellationToken = default);

    Task<HlsOutputResult> GenerateHlsAsync(
        Guid videoId,
        string sourceStoragePath,
        MediaProbeResult sourceMetadata,
        CancellationToken cancellationToken = default);

    Task<ThumbnailOutputResult> GenerateThumbnailAsync(
        Guid videoId,
        string sourceStoragePath,
        MediaProbeResult sourceMetadata,
        CancellationToken cancellationToken = default);
}
