using System.Text;
using StreamForge.Application.Interfaces;

namespace StreamForge.Api.IntegrationTests.Support;

internal static class PlaybackTestMedia
{
    internal const string HlsContentType = "application/vnd.apple.mpegurl";
    internal const string VariantDirectory = "360p";
    internal const string PlaylistFileName = "index.m3u8";
    internal const string SegmentFileName = "segment_00000.ts";
    internal const string PlaylistPath = $"{VariantDirectory}/{PlaylistFileName}";
    internal const string SegmentPath = $"{VariantDirectory}/{SegmentFileName}";
    internal const string MasterHeader = "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=400000,RESOLUTION=640x360\n";
    internal const string VariantPlaylist = $"#EXTM3U\n#EXT-X-TARGETDURATION:6\n#EXTINF:6.0,\n{SegmentFileName}\n#EXT-X-ENDLIST\n";
    internal static readonly byte[] SegmentBytes = [0x47, 0x20, 0x00, 0x10, 0xA5];
    internal static readonly byte[] ThumbnailBytes = [0xFF, 0xD8, 0xFF, 0xD9];
    internal static readonly MediaProbeResult Metadata = new(6, 640, 360, 400, "h264");

    internal static async Task<HlsOutputResult> WriteHlsAsync(
        IStorageService storage, Guid videoId, CancellationToken cancellationToken = default)
    {
        var hlsPath = Path.Combine("processing", videoId.ToString("N"), "hls");
        var masterBytes = Encoding.UTF8.GetBytes($"{MasterHeader}{PlaylistPath}\n");
        var playlistBytes = Encoding.UTF8.GetBytes(VariantPlaylist);
        var masterPath = await ImportAsync(storage, Path.Combine(hlsPath, "master.m3u8"), masterBytes, cancellationToken);
        var playlistPath = await ImportAsync(storage, Path.Combine(hlsPath, VariantDirectory, PlaylistFileName), playlistBytes, cancellationToken);
        await ImportAsync(storage, Path.Combine(hlsPath, VariantDirectory, SegmentFileName), SegmentBytes, cancellationToken);

        var variant = new HlsVariantResult(VariantDirectory, Metadata.Width, Metadata.Height,
            playlistPath, playlistBytes.Length, Metadata.Bitrate, Metadata.Codec);
        return new HlsOutputResult(masterPath, masterBytes.Length, [variant]);
    }

    internal static async Task<ThumbnailOutputResult> WriteThumbnailAsync(
        IStorageService storage, Guid videoId, CancellationToken cancellationToken = default)
    {
        var path = await ImportAsync(storage, Path.Combine("processing", videoId.ToString("N"), "thumbnail.jpg"),
            ThumbnailBytes, cancellationToken);
        return new ThumbnailOutputResult(path, Metadata.Width, Metadata.Height, ThumbnailBytes.Length, TimestampSeconds: 0);
    }

    private static async Task<string> ImportAsync(
        IStorageService storage, string storagePath, byte[] bytes, CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(sourcePath, bytes, cancellationToken);
            return await storage.ImportFileAsync(sourcePath, storagePath, cancellationToken);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }
}

internal sealed class FakeMediaProcessingService(IStorageService storage) : IMediaProcessingService
{
    public async Task<MediaProbeResult> ProbeAsync(string sourceStoragePath, CancellationToken cancellationToken = default)
    {
        if (await storage.GetFileSizeAsync(sourceStoragePath, cancellationToken) == 0)
        {
            throw new InvalidOperationException("Expected a nonempty uploaded source file.");
        }

        return PlaybackTestMedia.Metadata;
    }

    public Task<HlsOutputResult> GenerateHlsAsync(
        Guid videoId, string sourceStoragePath, MediaProbeResult sourceMetadata, CancellationToken cancellationToken = default) =>
        PlaybackTestMedia.WriteHlsAsync(storage, videoId, cancellationToken);

    public Task<ThumbnailOutputResult> GenerateThumbnailAsync(
        Guid videoId, string sourceStoragePath, MediaProbeResult sourceMetadata, CancellationToken cancellationToken = default) =>
        PlaybackTestMedia.WriteThumbnailAsync(storage, videoId, cancellationToken);
}
