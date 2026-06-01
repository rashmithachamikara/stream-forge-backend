using Microsoft.Extensions.Logging;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Storage;

/// <summary>
/// Local filesystem storage provider
/// </summary>
public class LocalFileStorageService : IStorageService
{
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
        // Create session-specific directory
        var sessionDirectory = Path.Combine(_storagePath, sessionId.ToString());
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
        var endpoint = $"/api/uploads/sessions/{sessionId}/parts/{partNumber}";

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
        var finalDirectory = Path.Combine(_storagePath, sessionId.ToString(), "final");
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

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {Path}", fullPath);
        }

        await Task.CompletedTask;
    }

    public async Task DeleteRangeAsync(IEnumerable<string> storagePaths, CancellationToken cancellationToken = default)
    {
        foreach (var storagePath in storagePaths)
        {
            await DeleteAsync(storagePath, cancellationToken);
        }
    }

    public async Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, storagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fullPath}");
        }

        var fileInfo = new FileInfo(fullPath);
        return await Task.FromResult(fileInfo.Length);
    }

    public async Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, storagePath);
        return await Task.FromResult(File.Exists(fullPath));
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
