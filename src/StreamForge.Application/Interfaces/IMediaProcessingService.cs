namespace StreamForge.Application.Interfaces;

/// <summary>
/// Describes probed media metadata from the uploaded source file.
/// </summary>
public sealed record MediaProbeResult(
    int DurationSeconds,
    int Width,
    int Height,
    int? Bitrate,
    string? Codec);

/// <summary>
/// Describes a generated HLS rendition playlist and its technical metadata.
/// </summary>
public sealed record HlsVariantResult(
    string Resolution,
    int Width,
    int Height,
    string PlaylistPath,
    long PlaylistSize,
    int? Bitrate,
    string? Codec);

/// <summary>
/// Describes the generated HLS master playlist and its available renditions.
/// </summary>
public sealed record HlsOutputResult(
    string MasterPlaylistPath,
    long MasterPlaylistSize,
    IReadOnlyCollection<HlsVariantResult> Variants);

/// <summary>
/// Describes a generated thumbnail artifact for a video.
/// </summary>
public sealed record ThumbnailOutputResult(
    string StoragePath,
    int Width,
    int Height,
    long SizeBytes,
    int TimestampSeconds);

/// <summary>
/// Abstraction over media probing and derived-asset generation such as HLS and thumbnails.
/// </summary>
public interface IMediaProcessingService
{
    /// <summary>
    /// Reads technical metadata from the source media file.
    /// </summary>
    /// <param name="sourceStoragePath">Provider-relative storage path for the source file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The probed media metadata.</returns>
    Task<MediaProbeResult> ProbeAsync(string sourceStoragePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates HLS playlists and segments for the supplied source media.
    /// </summary>
    /// <param name="videoId">Video identifier that owns the generated artifacts.</param>
    /// <param name="sourceStoragePath">Provider-relative storage path for the source file.</param>
    /// <param name="sourceMetadata">Previously probed source metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated HLS output and rendition metadata.</returns>
    Task<HlsOutputResult> GenerateHlsAsync(
        Guid videoId,
        string sourceStoragePath,
        MediaProbeResult sourceMetadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thumbnail image for the supplied source media.
    /// </summary>
    /// <param name="videoId">Video identifier that owns the generated artifact.</param>
    /// <param name="sourceStoragePath">Provider-relative storage path for the source file.</param>
    /// <param name="sourceMetadata">Previously probed source metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated thumbnail metadata.</returns>
    Task<ThumbnailOutputResult> GenerateThumbnailAsync(
        Guid videoId,
        string sourceStoragePath,
        MediaProbeResult sourceMetadata,
        CancellationToken cancellationToken = default);
}
