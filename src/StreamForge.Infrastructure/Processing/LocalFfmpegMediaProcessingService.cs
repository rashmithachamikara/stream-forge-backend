using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Processing;

public sealed class LocalFfmpegMediaProcessingService : IMediaProcessingService
{
    private readonly string _storageRoot;
    private readonly VideoProcessingOptions _options;
    private readonly ILogger<LocalFfmpegMediaProcessingService> _logger;

    public LocalFfmpegMediaProcessingService(
        string storageRoot,
        VideoProcessingOptions options,
        ILogger<LocalFfmpegMediaProcessingService> logger)
    {
        _storageRoot = Path.GetFullPath(storageRoot);
        _options = options;
        _logger = logger;
    }

    public async Task<MediaProbeResult> ProbeAsync(string sourceStoragePath, CancellationToken cancellationToken = default)
    {
        var sourcePath = ResolveStoragePath(sourceStoragePath);
        var result = await RunProcessAsync(
            _options.FfprobePath,
            new[]
            {
                "-v", "error",
                "-select_streams", "v:0",
                "-show_entries", "stream=codec_name,width,height,bit_rate,duration",
                "-show_format",
                "-of", "json",
                sourcePath
            },
            cancellationToken);

        using var document = JsonDocument.Parse(result.StandardOutput);
        var stream = document.RootElement.GetProperty("streams").EnumerateArray().FirstOrDefault();
        if (stream.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("ffprobe did not return a video stream");
        }

        var format = document.RootElement.TryGetProperty("format", out var formatElement) ? formatElement : default;
        var duration = ReadDouble(stream, "duration") ?? ReadDouble(format, "duration") ?? 0;
        var bitrate = ReadInt(stream, "bit_rate") ?? ReadInt(format, "bit_rate");

        return new MediaProbeResult(
            DurationSeconds: (int)Math.Ceiling(duration),
            Width: ReadInt(stream, "width") ?? 0,
            Height: ReadInt(stream, "height") ?? 0,
            Bitrate: bitrate.HasValue ? bitrate.Value / 1000 : null,
            Codec: ReadString(stream, "codec_name"));
    }

    public async Task<HlsOutputResult> GenerateHlsAsync(
        Guid videoId,
        string sourceStoragePath,
        MediaProbeResult sourceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = ResolveStoragePath(sourceStoragePath);
        var hlsRelativeRoot = Path.Combine("processing", videoId.ToString("N"), "hls");
        var hlsRoot = ResolveStoragePath(hlsRelativeRoot);
        Directory.CreateDirectory(hlsRoot);

        var variants = SelectVariants(sourceMetadata.Height).ToArray();
        var results = new List<HlsVariantResult>();

        foreach (var variant in variants)
        {
            var variantWidth = CalculateVariantWidth(sourceMetadata.Width, sourceMetadata.Height, variant.Height);
            var variantDirectory = Path.Combine(hlsRoot, variant.Name);
            Directory.CreateDirectory(variantDirectory);
            var playlistPath = Path.Combine(variantDirectory, "index.m3u8");
            var segmentPattern = Path.Combine(variantDirectory, "segment_%05d.ts");
            var videoEncodingArguments = BuildVideoEncodingArguments(variant.Bitrate);

            _logger.LogInformation(
                "Generating HLS variant {Resolution} for video {VideoId}",
                variant.Name,
                videoId);

            await RunProcessAsync(
                _options.FfmpegPath,
                new[]
                {
                    "-y",
                    "-i", sourcePath,
                    "-vf", $"scale=-2:{variant.Height}",
                    "-c:v", "libx264",
                    "-preset", "fast",
                }
                .Concat(videoEncodingArguments)
                .Concat(
                [
                    "-c:a", "aac",
                    "-b:a", "128k",
                    "-hls_time", _options.HlsSegmentSeconds.ToString(CultureInfo.InvariantCulture),
                    "-hls_playlist_type", "vod",
                    "-hls_segment_filename", segmentPattern,
                    playlistPath
                ]),
                cancellationToken);

            results.Add(new HlsVariantResult(
                variant.Name,
                variantWidth,
                variant.Height,
                ToStoragePath(playlistPath),
                new FileInfo(playlistPath).Length,
                variant.Bitrate,
                "h264"));

            _logger.LogInformation(
                "Generated HLS variant {Resolution} for video {VideoId} at {PlaylistPath}",
                variant.Name,
                videoId,
                ToStoragePath(playlistPath));
        }

        var masterPath = Path.Combine(hlsRoot, "master.m3u8");
        await File.WriteAllLinesAsync(
            masterPath,
            BuildMasterPlaylist(results),
            cancellationToken);

        return new HlsOutputResult(ToStoragePath(masterPath), new FileInfo(masterPath).Length, results);
    }

    public async Task<ThumbnailOutputResult> GenerateThumbnailAsync(
        Guid videoId,
        string sourceStoragePath,
        MediaProbeResult sourceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = ResolveStoragePath(sourceStoragePath);
        var thumbnailRelativeDirectory = Path.Combine("processing", videoId.ToString("N"), "thumbnails");
        var thumbnailDirectory = ResolveStoragePath(thumbnailRelativeDirectory);
        Directory.CreateDirectory(thumbnailDirectory);

        var timestamp = Math.Max(0, sourceMetadata.DurationSeconds * _options.ThumbnailTimestampPercent / 100);
        var thumbnailPath = Path.Combine(thumbnailDirectory, "default.jpg");
        await RunProcessAsync(
            _options.FfmpegPath,
            new[]
            {
                "-y",
                "-ss", timestamp.ToString(CultureInfo.InvariantCulture),
                "-i", sourcePath,
                "-frames:v", "1",
                "-q:v", "2",
                thumbnailPath
            },
            cancellationToken);

        return new ThumbnailOutputResult(
            ToStoragePath(thumbnailPath),
            sourceMetadata.Width,
            sourceMetadata.Height,
            new FileInfo(thumbnailPath).Length,
            timestamp);
    }

    private string ResolveStoragePath(string storagePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, storagePath));
        if (!fullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved path is outside the configured storage root");
        }

        return fullPath;
    }

    private string ToStoragePath(string fullPath) =>
        Path.GetRelativePath(_storageRoot, fullPath).Replace('\\', '/');

    private static IEnumerable<(string Name, int Height, int? Bitrate)> SelectVariants(int sourceHeight)
    {
        var variants = new[]
        {
            ("1080p", 1080, (int?)4600),
            ("720p", 720, (int?)2800),
            ("480p", 480, (int?)1400)
        };

        var selected = variants.Where(variant => sourceHeight >= variant.Item2).ToArray();
        if (selected.Length > 0)
        {
            return selected;
        }

        var fallbackHeight = Math.Max(2, sourceHeight - sourceHeight % 2);
        return new[] { ($"{fallbackHeight}p", fallbackHeight, (int?)null) };
    }

    private static IEnumerable<string> BuildMasterPlaylist(IEnumerable<HlsVariantResult> variants)
    {
        yield return "#EXTM3U";
        yield return "#EXT-X-VERSION:3";
        foreach (var variant in variants)
        {
            var bandwidth = Math.Max(1, (variant.Bitrate ?? 1400) * 1000);
            yield return $"#EXT-X-STREAM-INF:BANDWIDTH={bandwidth},RESOLUTION={variant.Width}x{variant.Height}";
            yield return $"{variant.Resolution}/index.m3u8";
        }
    }

    private static int CalculateVariantWidth(int sourceWidth, int sourceHeight, int targetHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return targetHeight * 16 / 9;
        }

        var width = (int)Math.Round((double)sourceWidth * targetHeight / sourceHeight);
        return Math.Max(2, width - width % 2);
    }

    private static IEnumerable<string> BuildVideoEncodingArguments(int? bitrateKbps)
    {
        if (!bitrateKbps.HasValue || bitrateKbps.Value <= 0)
        {
            return
            [
                "-crf", "22"
            ];
        }

        var targetBitrate = bitrateKbps.Value;
        var maxRate = targetBitrate;
        var bufferSize = targetBitrate * 2;

        var crf = targetBitrate switch
        {
            >= 5000 => 21, // 1080p
            >= 3000 => 22, // 720p
            _ => 23        // 480p
        };

        return
        [
            "-crf", crf.ToString(CultureInfo.InvariantCulture),
            "-maxrate", $"{maxRate}k",
            "-bufsize", $"{bufferSize}k"
        ];
    }

    private async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var result = new ProcessResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "Media process {FileName} failed with exit code {ExitCode}: {StandardError}",
                fileName,
                result.ExitCode,
                result.StandardError);
            throw new InvalidOperationException($"{fileName} failed with exit code {result.ExitCode}");
        }

        return result;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
