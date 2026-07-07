using System.Security.Cryptography;
using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Uploads;
using StreamForge.Application.UseCases.Uploads.CreateSession;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Uploads;

public sealed class UploadSessionUseCaseTests
{
    [Fact]
    public async Task CreateUploadSession_ShouldCreateVideoAndSession()
    {
        var userId = Guid.NewGuid();
        var videos = Substitute.For<IVideoRepository>();
        var uploadSessions = Substitute.For<IUploadSessionRepository>();
        var service = new CreateUploadSessionService(
            CreateUnitOfWork(videos: videos, uploadSessions: uploadSessions),
            CreateCurrentUser(userId),
            CreateUploadOptions(),
            CreateStorageOptions());

        var result = await service.Handle(CreateUploadRequest(), CancellationToken.None);

        result.VideoTitle.Should().Be("Sample Video");
        result.SessionId.Should().NotBeEmpty();
        result.VideoId.Should().NotBeEmpty();
        await videos.Received(1).AddAsync(Arg.Is<Video>(video =>
            video.Id == result.VideoId &&
            video.UploaderId == userId &&
            video.Status == VideoStatus.Uploading), Arg.Any<CancellationToken>());
        await uploadSessions.Received(1).AddAsync(Arg.Is<UploadSession>(session =>
            session.Id == result.SessionId &&
            session.UserId == userId &&
            session.VideoId == result.VideoId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUploadSession_ShouldRequireAuthenticatedUser()
    {
        var service = new CreateUploadSessionService(
            CreateUnitOfWork(),
            Substitute.For<ICurrentUserService>(),
            CreateUploadOptions(),
            CreateStorageOptions());

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
            CreateUploadOptions(maxFileSize: 100),
            CreateStorageOptions());

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
            CreateUploadOptions(),
            CreateStorageOptions());

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
            CreateUploadOptions(),
            CreateStorageOptions());

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
    public async Task GetUploadTarget_ShouldReturnStorageTargetAndMarkSessionActive()
    {
        var userId = Guid.NewGuid();
        var session = UploadSession.Create(userId, Guid.NewGuid(), 3, StorageProviderType.Local, "tmp", "video/mp4");
        var uploadSessions = Substitute.For<IUploadSessionRepository>();
        uploadSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var storage = Substitute.For<IStorageService>();
        storage.GetUploadTargetAsync(session.Id, 1, 3, Arg.Any<CancellationToken>())
            .Returns(new UploadTarget
            {
                Type = StorageTargetType.BackendEndpoint,
                Url = "/api/uploads/part",
                HttpMethod = "POST",
                Headers = new Dictionary<string, string> { ["x-test"] = "1" }
            });
        var service = new GetUploadTargetService(
            CreateUnitOfWork(uploadSessions: uploadSessions),
            CreateCurrentUser(userId),
            storage,
            CreateUploadOptions());

        var result = await service.Handle(new GetUploadTargetCommand(session.Id, PartNumber: 1, PartSize: 3), CancellationToken.None);

        result.Type.Should().Be(nameof(StorageTargetType.BackendEndpoint));
        result.Url.Should().Be("/api/uploads/part");
        result.HttpMethod.Should().Be("POST");
        result.Headers.Should().ContainKey("x-test");
        session.Status.Should().Be(UploadSessionStatus.Active);
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
    public async Task UploadPart_ShouldSavePartAndUpdateSessionProgress()
    {
        var userId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };
        var checksum = Sha256(content);
        var session = UploadSession.Create(userId, Guid.NewGuid(), 3, StorageProviderType.Local, "tmp", "video/mp4");
        var uploadSessions = Substitute.For<IUploadSessionRepository>();
        uploadSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var uploadParts = Substitute.For<IUploadSessionPartRepository>();
        uploadParts.GetBySessionAndPartAsync(session.Id, 1, Arg.Any<CancellationToken>()).Returns((UploadSessionPart?)null);
        var storage = Substitute.For<IStorageService>();
        storage.SavePartAsync(session.Id, 1, Arg.Any<Stream>(), "video.mp4", Arg.Any<CancellationToken>())
            .Returns("parts/1");
        var service = new UploadPartService(
            CreateUnitOfWork(uploadSessions: uploadSessions, uploadSessionParts: uploadParts),
            CreateCurrentUser(userId),
            storage,
            CreateUploadOptions());

        var result = await service.Handle(
            new UploadPartCommand(session.Id, PartNumber: 1, new MemoryStream(content), "video.mp4", FileLength: 3, checksum),
            CancellationToken.None);

        result.SessionId.Should().Be(session.Id);
        result.PartNumber.Should().Be(1);
        result.IsComplete.Should().BeTrue();
        session.UploadedSize.Should().Be(3);
        session.Status.Should().Be(UploadSessionStatus.Active);
        await uploadParts.Received(1).AddAsync(Arg.Is<UploadSessionPart>(part =>
            part.UploadSessionId == session.Id &&
            part.PartNumber == 1 &&
            part.Size == 3 &&
            part.Checksum == checksum &&
            part.StoragePath == "parts/1" &&
            part.IsComplete), Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task CompleteUploadSession_ShouldCreateSourceRecordsAndEnqueueProcessing()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Sample Video", null, userId, status: VideoStatus.Uploading);
        var session = UploadSession.Create(userId, video.Id, 3, StorageProviderType.Local, "tmp", "video/mp4");
        var part = UploadSessionPart.Create(session.Id, 1, 3, "checksum", "parts/1");
        part.MarkAsComplete();
        var provider = StorageProvider.Create("local", StorageProviderType.Local, "{}", isDefault: true);

        var uploadSessions = Substitute.For<IUploadSessionRepository>();
        uploadSessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var uploadParts = Substitute.For<IUploadSessionPartRepository>();
        uploadParts.GetBySessionIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns([part]);
        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var storageProviders = Substitute.For<IStorageProviderRepository>();
        storageProviders.GetDefaultByTypeAsync(StorageProviderType.Local, Arg.Any<CancellationToken>()).Returns(provider);
        var videoVersions = Substitute.For<IVideoVersionRepository>();
        var videoFiles = Substitute.For<IVideoFileRepository>();
        var processingJobs = Substitute.For<IVideoProcessingJobRepository>();
        var storage = Substitute.For<IStorageService>();
        storage.AssembleChunksAsync(session.Id, Arg.Any<IEnumerable<string>>(), "video.mp4", Arg.Any<CancellationToken>())
            .Returns("assembled/video.mp4");
        storage.GetFileSizeAsync("assembled/video.mp4", Arg.Any<CancellationToken>()).Returns(3);
        storage.CalculateChecksumAsync("assembled/video.mp4", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("final-checksum");
        storage.PromoteCompletedUploadAsync(video.Id, "assembled/video.mp4", "video.mp4", Arg.Any<CancellationToken>())
            .Returns("videos/source/video.mp4");
        storage.GetFileSizeAsync("videos/source/video.mp4", Arg.Any<CancellationToken>()).Returns(3);
        var queue = Substitute.For<IVideoProcessingQueue>();
        var service = new CompleteUploadSessionService(
            CreateUnitOfWork(
                uploadSessions: uploadSessions,
                uploadSessionParts: uploadParts,
                videos: videos,
                storageProviders: storageProviders,
                videoVersions: videoVersions,
                videoFiles: videoFiles,
                videoProcessingJobs: processingJobs),
            CreateCurrentUser(userId),
            storage,
            queue);

        var result = await service.Handle(new CompleteUploadSessionCommand(session.Id, "video.mp4"), CancellationToken.None);

        result.SessionId.Should().Be(session.Id);
        result.VideoId.Should().Be(video.Id);
        result.Status.Should().Be(nameof(UploadSessionStatus.Completed));
        session.Status.Should().Be(UploadSessionStatus.Completed);
        video.Status.Should().Be(VideoStatus.Processing);
        await videoVersions.Received(1).AddAsync(Arg.Is<VideoVersion>(version =>
            version.VideoId == video.Id &&
            version.StoragePath == "videos/source/video.mp4" &&
            version.SizeBytes == 3), Arg.Any<CancellationToken>());
        await videoFiles.Received(1).AddAsync(Arg.Is<VideoFile>(file =>
            file.StorageProviderId == provider.Id &&
            file.FilePath == "videos/source/video.mp4" &&
            file.FileSize == 3 &&
            file.Checksum == "final-checksum"), Arg.Any<CancellationToken>());
        await processingJobs.Received(1).AddAsync(Arg.Any<VideoProcessingJob>(), Arg.Any<CancellationToken>());
        await queue.Received(1).EnqueueAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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
            SessionExpirationMinutes = 60
        };
    }

    private static StorageOptions CreateStorageOptions(string providerType = "local")
    {
        return new StorageOptions
        {
            ProviderType = providerType
        };
    }

    private static IUnitOfWork CreateUnitOfWork(
        ICategoryRepository? categories = null,
        IVideoRepository? videos = null,
        IUploadSessionRepository? uploadSessions = null,
        IUploadSessionPartRepository? uploadSessionParts = null,
        IStorageProviderRepository? storageProviders = null,
        IVideoVersionRepository? videoVersions = null,
        IVideoFileRepository? videoFiles = null,
        IVideoProcessingJobRepository? videoProcessingJobs = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Categories.Returns(categories ?? Substitute.For<ICategoryRepository>());
        unitOfWork.Tags.Returns(Substitute.For<ITagRepository>());
        unitOfWork.Videos.Returns(videos ?? Substitute.For<IVideoRepository>());
        unitOfWork.UploadSessions.Returns(uploadSessions ?? Substitute.For<IUploadSessionRepository>());
        unitOfWork.UploadSessionParts.Returns(uploadSessionParts ?? Substitute.For<IUploadSessionPartRepository>());
        unitOfWork.StorageProviders.Returns(storageProviders ?? Substitute.For<IStorageProviderRepository>());
        unitOfWork.VideoVersions.Returns(videoVersions ?? Substitute.For<IVideoVersionRepository>());
        unitOfWork.VideoFiles.Returns(videoFiles ?? Substitute.For<IVideoFileRepository>());
        unitOfWork.VideoProcessingJobs.Returns(videoProcessingJobs ?? Substitute.For<IVideoProcessingJobRepository>());
        return unitOfWork;
    }

    private static ICurrentUserService CreateCurrentUser(Guid? userId = null)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId ?? Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Editor);
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }

    private static string Sha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }
}
