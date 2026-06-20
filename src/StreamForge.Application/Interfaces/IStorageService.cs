namespace StreamForge.Application.Interfaces;

/// <summary>
/// Storage target type
/// </summary>
public enum StorageTargetType
{
    /// <summary>
    /// Direct backend upload endpoint
    /// </summary>
    BackendEndpoint = 0,

    /// <summary>
    /// Pre-signed S3 URL
    /// </summary>
    S3PresignedUrl = 1
}

/// <summary>
/// Represents where a client should upload a video file
/// </summary>
public class UploadTarget
{
    /// <summary>
    /// Type of target
    /// </summary>
    public StorageTargetType Type { get; set; }

    /// <summary>
    /// The URL or endpoint where the client should upload
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Optional headers to include in the request (for S3)
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional HTTP method (default: PUT for S3, POST for backend)
    /// </summary>
    public string? HttpMethod { get; set; }
}

public sealed record StoredFileDescriptor(
    Stream Stream,
    string ContentType,
    string FileName,
    long? Length);

/// <summary>
/// Abstraction for file storage operations
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Saves a file to storage
    /// </summary>
    /// <param name="sessionId">Upload session ID</param>
    /// <param name="partNumber">Part number in chunked upload</param>
    /// <param name="fileStream">File stream to save</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage path where the file was saved</returns>
    Task<string> SavePartAsync(
        Guid sessionId,
        int partNumber,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the upload target for a client to upload a chunk
    /// </summary>
    /// <param name="sessionId">Upload session ID</param>
    /// <param name="partNumber">Part number in chunked upload</param>
    /// <param name="fileSize">Size of the file part in bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload target with URL and method</returns>
    Task<UploadTarget> GetUploadTargetAsync(
        Guid sessionId,
        int partNumber,
        long fileSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assembles uploaded chunks into a final file
    /// </summary>
    /// <param name="sessionId">Upload session ID</param>
    /// <param name="partPaths">Paths to all parts in order</param>
    /// <param name="finalFileName">Final output file name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the assembled final file</returns>
    Task<string> AssembleChunksAsync(
        Guid sessionId,
        IEnumerable<string> partPaths,
        string finalFileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes a validated completed upload source into canonical video storage.
    /// </summary>
    /// <param name="videoId">Video ID that owns the completed source file</param>
    /// <param name="sourceStoragePath">Provider-relative path or key for the assembled upload source</param>
    /// <param name="fileName">Final source file name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider-relative path or key for the permanent video source file</returns>
    Task<string> PromoteCompletedUploadAsync(
        Guid videoId,
        string sourceStoragePath,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="storagePath">Storage path to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple files from storage
    /// </summary>
    /// <param name="storagePaths">Storage paths to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRangeAsync(IEnumerable<string> storagePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size of a stored file
    /// </summary>
    /// <param name="storagePath">Storage path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File size in bytes</returns>
    Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates a checksum for a stored file.
    /// </summary>
    /// <param name="storagePath">Storage path</param>
    /// <param name="algorithm">Checksum algorithm. Defaults to SHA-256.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hex-encoded checksum</returns>
    Task<string> CalculateChecksumAsync(
        string storagePath,
        string algorithm = "SHA256",
        CancellationToken cancellationToken = default);

    Task<StoredFileDescriptor> OpenReadAsync(
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> ImportFileAsync(
        string sourceFilePath,
        string destinationStoragePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="storagePath">Storage path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file exists</returns>
    Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);
}
