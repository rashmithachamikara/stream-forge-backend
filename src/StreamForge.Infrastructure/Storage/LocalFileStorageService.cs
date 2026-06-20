using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Storage;

/// <summary>
/// Local filesystem storage provider
/// </summary>
public class LocalFileStorageService : IStorageService
{
    private static readonly HashSet<string> ProtectedTopLevelDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "sessions",
        "videos",
        "processing"
    };

    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(string storagePath, ILogger<LocalFileStorageService> logger)
    {
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure storage path exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created storage directory: {StoragePath}", _storagePath);
        }
    }

    public async Task<string> SavePartAsync(
        Guid sessionId,
        int partNumber,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Create session-specific temporary directory
        var sessionDirectory = GetSessionDirectory(sessionId);
        if (!Directory.Exists(sessionDirectory))
        {
            Directory.CreateDirectory(sessionDirectory);
        }

        // Generate safe part file name
        var safeFileName = SanitizeFileName(fileName);
        var partFileName = $"part_{partNumber:D5}_{safeFileName}";
        var partPath = Path.Combine(sessionDirectory, partFileName);

        // Save the part
        using (var fileStream_ = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(fileStream_, cancellationToken);
        }

        _logger.LogInformation("Saved part {PartNumber} for session {SessionId} to {PartPath}", partNumber, sessionId, partPath);

        // Return relative path from storage root
        return Path.GetRelativePath(_storagePath, partPath);
    }

    public Task<UploadTarget> GetUploadTargetAsync(
        Guid sessionId,
        int partNumber,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        // For local filesystem, return a backend endpoint URL
        // The actual endpoint implementation will handle the upload
        var endpoint = $"/api/v1/uploads/sessions/{sessionId}/parts/{partNumber}";

        var target = new UploadTarget
        {
            Type = StorageTargetType.BackendEndpoint,
            Url = endpoint,
            HttpMethod = "POST"
        };

        return Task.FromResult(target);
    }

    public async Task<string> AssembleChunksAsync(
        Guid sessionId,
        IEnumerable<string> partPaths,
        string finalFileName,
        CancellationToken cancellationToken = default)
    {
        var finalDirectory = Path.Combine(GetSessionDirectory(sessionId), "final");
        if (!Directory.Exists(finalDirectory))
        {
            Directory.CreateDirectory(finalDirectory);
        }

        var safeFileName = SanitizeFileName(finalFileName);
        var finalPath = Path.Combine(finalDirectory, safeFileName);

        // Assemble parts in order
        using (var finalFile = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            foreach (var partPath in partPaths)
            {
                var fullPartPath = Path.Combine(_storagePath, partPath);
                if (!File.Exists(fullPartPath))
                {
                    throw new FileNotFoundException($"Part file not found: {fullPartPath}");
                }

                using (var partFile = new FileStream(fullPartPath, FileMode.Open, FileAccess.Read))
                {
                    await partFile.CopyToAsync(finalFile, cancellationToken);
                }
            }
        }

        _logger.LogInformation("Assembled {PartCount} parts for session {SessionId} to {FinalPath}", partPaths.Count(), sessionId, finalPath);

        // Return relative path
        return Path.GetRelativePath(_storagePath, finalPath);
    }

    public Task<string> PromoteCompletedUploadAsync(
        Guid videoId,
        string sourceStoragePath,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = ResolveStoragePath(sourceStoragePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Completed upload source file not found: {sourcePath}");
        }

        var safeFileName = SanitizeFileName(Path.GetFileName(fileName));
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "source.mp4";
        }

        var destinationDirectory = ResolveStoragePath(Path.Combine("videos", videoId.ToString("N"), "original"));
        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, safeFileName));
        EnsureInsideStorageRoot(destinationPath);
        if (File.Exists(destinationPath))
        {
            throw new IOException($"Permanent video source already exists: {destinationPath}");
        }

        File.Move(sourcePath, destinationPath);
        _logger.LogInformation(
            "Promoted completed upload source for video {VideoId} from {SourcePath} to {DestinationPath}",
            videoId,
            sourcePath,
            destinationPath);

        return Task.FromResult(Path.GetRelativePath(_storagePath, destinationPath));
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveStoragePath(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {Path}", fullPath);
        }

        await Task.CompletedTask;
    }

    public async Task DeleteRangeAsync(IEnumerable<string> storagePaths, CancellationToken cancellationToken = default)
    {
        var touchedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var storagePath in storagePaths)
        {
            var fullPath = ResolveStoragePath(storagePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                touchedDirectories.Add(directory);
            }

            try
            {
                await DeleteAsync(storagePath, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete temporary upload chunk {StoragePath}",
                    storagePath);
            }
        }

        foreach (var directory in touchedDirectories.OrderByDescending(directory => directory.Length))
        {
            PruneEmptyDirectories(directory);
        }
    }

    public async Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveStoragePath(storagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fullPath}");
        }

        var fileInfo = new FileInfo(fullPath);
        return await Task.FromResult(fileInfo.Length);
    }

    public async Task<string> CalculateChecksumAsync(
        string storagePath,
        string algorithm = "SHA256",
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(algorithm, "SHA256", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Checksum algorithm '{algorithm}' is not supported by local storage");
        }

        var fullPath = ResolveStoragePath(storagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fullPath}");
        }

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            useAsync: true);

        var checksum = await SHA256.HashDataAsync(fileStream, cancellationToken);
        return Convert.ToHexString(checksum).ToLowerInvariant();
    }

    public Task<StoredFileDescriptor> OpenReadAsync(
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveStoragePath(storagePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fullPath}");
        }

        var fileInfo = new FileInfo(fullPath);
        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            useAsync: true);

        return Task.FromResult(new StoredFileDescriptor(stream, contentType, Path.GetFileName(fullPath), fileInfo.Length));
    }

    public Task<string> ImportFileAsync(
        string sourceFilePath,
        string destinationStoragePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path is required.", nameof(sourceFilePath));
        }

        if (string.IsNullOrWhiteSpace(destinationStoragePath))
        {
            throw new ArgumentException("Destination storage path is required.", nameof(destinationStoragePath));
        }

        var sourcePath = Path.IsPathRooted(sourceFilePath)
            ? Path.GetFullPath(sourceFilePath)
            : ResolveStoragePath(sourceFilePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source artifact file not found: {sourcePath}");
        }

        var destinationPath = ResolveStoragePath(destinationStoragePath);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Destination directory could not be determined.");
        Directory.CreateDirectory(destinationDirectory);

        File.Copy(sourcePath, destinationPath, overwrite: true);
        _logger.LogInformation(
            "Imported transcription artifact from {SourcePath} to {DestinationPath}",
            sourcePath,
            destinationPath);

        return Task.FromResult(Path.GetRelativePath(_storagePath, destinationPath));
    }

    public async Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveStoragePath(storagePath);
        return await Task.FromResult(File.Exists(fullPath));
    }

    private string ResolveStoragePath(string storagePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_storagePath, storagePath));
        EnsureInsideStorageRoot(fullPath);
        return fullPath;
    }

    private string GetSessionDirectory(Guid sessionId)
    {
        return ResolveStoragePath(Path.Combine("sessions", sessionId.ToString()));
    }

    private void EnsureInsideStorageRoot(string fullPath)
    {
        var storageRoot = Path.GetFullPath(_storagePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.Equals(fullPath, storageRoot, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage path is outside the configured storage root");
        }
    }

    private void PruneEmptyDirectories(string? directory)
    {
        var storageRoot = Path.GetFullPath(_storagePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var current = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFullPath(directory);

        while (!string.IsNullOrWhiteSpace(current) &&
               !string.Equals(current, storageRoot, StringComparison.OrdinalIgnoreCase) &&
               current.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            if (IsProtectedTopLevelDirectory(current, storageRoot))
            {
                break;
            }

            if (!Directory.Exists(current))
            {
                current = Directory.GetParent(current)?.FullName;
                continue;
            }

            if (Directory.EnumerateFileSystemEntries(current).Any())
            {
                break;
            }

            try
            {
                Directory.Delete(current);
                _logger.LogInformation("Deleted empty upload directory: {Directory}", current);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete empty upload directory {Directory}",
                    current);
                break;
            }

            current = Directory.GetParent(current)?.FullName;
        }
    }

    private static bool IsProtectedTopLevelDirectory(string directory, string storageRoot)
    {
        var relativePath = Path.GetRelativePath(storageRoot, directory);
        return !relativePath.Contains(Path.DirectorySeparatorChar) &&
               !relativePath.Contains(Path.AltDirectorySeparatorChar) &&
               ProtectedTopLevelDirectories.Contains(relativePath);
    }

    /// <summary>
    /// Sanitizes a filename to prevent directory traversal and invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Remove path separators and other problematic characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(fileName.Split(invalidChars));

        // Limit length
        const int maxLength = 200;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return sanitized;
    }
}
