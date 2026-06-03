using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for UploadSession entity
/// </summary>
public interface IUploadSessionRepository : IRepository<UploadSession>
{
    Task<PagedQueryResult<UploadSession>> GetByUserIdPagedAsync(
        Guid userId,
        UploadSessionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sessions by user ID
    /// </summary>
    Task<IEnumerable<UploadSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active sessions for a user
    /// </summary>
    Task<IEnumerable<UploadSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sessions by status
    /// </summary>
    Task<IEnumerable<UploadSession>> GetByStatusAsync(UploadSessionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired sessions
    /// </summary>
    Task<IEnumerable<UploadSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by ID with all parts loaded
    /// </summary>
    Task<UploadSession?> GetWithPartsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for UploadSessionPart entity
/// </summary>
public interface IUploadSessionPartRepository : IRepository<UploadSessionPart>
{
    /// <summary>
    /// Gets all parts for a session
    /// </summary>
    Task<IEnumerable<UploadSessionPart>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific part by session ID and part number
    /// </summary>
    Task<UploadSessionPart?> GetBySessionAndPartAsync(Guid sessionId, int partNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all complete parts for a session
    /// </summary>
    Task<IEnumerable<UploadSessionPart>> GetCompletePartsBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of complete parts for a session
    /// </summary>
    Task<int> GetCompletePartCountAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all parts for a session
    /// </summary>
    Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
