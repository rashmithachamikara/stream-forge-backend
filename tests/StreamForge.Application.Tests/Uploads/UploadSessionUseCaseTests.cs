using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Uploads;
using StreamForge.Application.UseCases.Uploads.CreateSession;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Uploads;

public sealed class UploadSessionUseCaseTests
{
    [Fact]
    public async Task CreateUploadSession_ShouldRequireAuthenticatedUser()
    {
        var service = new CreateUploadSessionService(
            CreateUnitOfWork(),
            Substitute.For<ICurrentUserService>(),
            CreateUploadOptions());

        var request = CreateUploadRequest();

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<System.UnauthorizedAccessException>()
            .WithMessage("User must be authenticated to create upload session");
    }

    [Fact]
    public async Task CreateUploadSession_ShouldRejectOversizedFile()
    {
        var service = new CreateUploadSessionService(
            CreateUnitOfWork(),
            CreateCurrentUser(),
            CreateUploadOptions(maxFileSize: 100));

        var request = CreateUploadRequest(totalSize: 101);

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("TotalSize");
    }

    [Fact]
    public async Task CreateUploadSession_ShouldRejectUnsupportedMimeType()
    {
        var service = new CreateUploadSessionService(
            CreateUnitOfWork(),
            CreateCurrentUser(),
            CreateUploadOptions());

        var request = CreateUploadRequest(contentType: "application/octet-stream");

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("ContentType");
    }

    [Fact]
    public async Task CreateUploadSession_ShouldRejectMissingCategory()
    {
        var categories = Substitute.For<ICategoryRepository>();
        categories.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var service = new CreateUploadSessionService(
            CreateUnitOfWork(categories: categories),
            CreateCurrentUser(),
            CreateUploadOptions());

        var categoryId = Guid.NewGuid();
        var request = CreateUploadRequest(categoryId: categoryId);

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*Category*");
    }

    [Fact]
    public async Task GetUploadTarget_ShouldRejectInvalidPartNumber()
    {
        var service = new GetUploadTargetService(
            CreateUnitOfWork(),
            CreateCurrentUser(),
            Substitute.For<IStorageService>(),
            CreateUploadOptions());

        var act = () => service.Handle(new GetUploadTargetCommand(Guid.NewGuid(), PartNumber: 0, PartSize: 1), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("partNumber");
    }

    [Fact]
    public async Task UploadPart_ShouldRejectMissingChecksum()
    {
        var service = new UploadPartService(
            CreateUnitOfWork(),
            CreateCurrentUser(),
            Substitute.For<IStorageService>(),
            CreateUploadOptions());

        var command = new UploadPartCommand(
            Guid.NewGuid(),
            PartNumber: 1,
            new MemoryStream([1, 2, 3]),
            "video.mp4",
            FileLength: 3,
            Checksum: " ");

        var act = () => service.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("Checksum");
    }

    [Fact]
    public async Task CompleteUploadSession_ShouldRejectBlankFileName()
    {
        var service = new CompleteUploadSessionService(
            CreateUnitOfWork(),
            CreateCurrentUser(),
            Substitute.For<IStorageService>(),
            Substitute.For<IVideoProcessingQueue>());

        var act = () => service.Handle(new CompleteUploadSessionCommand(Guid.NewGuid(), " "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("FileName");
    }

    private static CreateUploadSessionRequest CreateUploadRequest(
        long totalSize = 100,
        string? contentType = "video/mp4",
        Guid? categoryId = null)
    {
        return new CreateUploadSessionRequest(
            "Sample Video",
            "Description",
            totalSize,
            contentType,
            categoryId,
            VideoVisibility.Private,
            TagIds: null);
    }

    private static UploadOptions CreateUploadOptions(long maxFileSize = 1_000, int chunkSize = 100)
    {
        return new UploadOptions
        {
            MaxFileSize = maxFileSize,
            ChunkSize = chunkSize,
            AllowedMimeTypes = ["video/mp4"],
            SessionExpirationMinutes = 60,
            StorageProviderType = "local"
        };
    }

    private static IUnitOfWork CreateUnitOfWork(ICategoryRepository? categories = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Categories.Returns(categories ?? Substitute.For<ICategoryRepository>());
        unitOfWork.Tags.Returns(Substitute.For<ITagRepository>());
        unitOfWork.Videos.Returns(Substitute.For<IVideoRepository>());
        unitOfWork.UploadSessions.Returns(Substitute.For<IUploadSessionRepository>());
        unitOfWork.UploadSessionParts.Returns(Substitute.For<IUploadSessionPartRepository>());
        return unitOfWork;
    }

    private static ICurrentUserService CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Editor);
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }
}
