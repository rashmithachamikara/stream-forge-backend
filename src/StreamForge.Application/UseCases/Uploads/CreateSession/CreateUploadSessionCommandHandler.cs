using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Uploads;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Uploads.CreateSession;

/// <summary>
/// Service for upload session operations
/// </summary>
public class CreateUploadSessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageService _storageService;
    private readonly UploadOptions _uploadOptions;

    public CreateUploadSessionService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IStorageService storageService,
        UploadOptions uploadOptions)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _storageService = storageService;
        _uploadOptions = uploadOptions;
    }

    public async Task<CreateUploadSessionResponseDto> Handle(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        // Validate user is authenticated
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to create upload session");
        }

        // Validate file size
        if (request.TotalSize > _uploadOptions.MaxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed ({_uploadOptions.MaxFileSize} bytes)");
        }

        if (request.TotalSize <= 0)
        {
            throw new InvalidOperationException("File size must be greater than 0");
        }

        // Validate MIME type if provided
        if (!string.IsNullOrEmpty(request.ContentType) &&
            !_uploadOptions.AllowedMimeTypes.Contains(request.ContentType))
        {
            throw new InvalidOperationException($"MIME type '{request.ContentType}' is not allowed");
        }

        // Verify category exists if specified
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _unitOfWork.Categories.ExistsAsync(request.CategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                throw new InvalidOperationException($"Category with ID {request.CategoryId} does not exist");
            }
        }

        // Create upload session
        var tempStoragePath = Path.Combine(Guid.NewGuid().ToString(), $"{request.Title}");
        var uploadSession = UploadSession.Create(
            userId: userId.Value,
            videoTitle: request.Title,
            totalSize: request.TotalSize,
            storageProviderType: StorageProviderType.Local,
            temporaryStoragePath: tempStoragePath,
            visibility: VideoVisibility.Private,
            videoDescription: request.Description,
            categoryId: request.CategoryId,
            contentType: request.ContentType,
            sessionExpirationMinutes: _uploadOptions.SessionExpirationMinutes);

        // Persist session
        await _unitOfWork.UploadSessions.AddAsync(uploadSession, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);


        return new CreateUploadSessionResponseDto(
            SessionId: uploadSession.Id,
            ExpiresAt: uploadSession.ExpiresAt,
            VideoTitle: uploadSession.VideoTitle);
    }
}
