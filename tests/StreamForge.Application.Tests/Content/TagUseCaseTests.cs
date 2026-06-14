using FluentAssertions;
using NSubstitute;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Content;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Content;

public sealed class TagUseCaseTests
{
    [Fact]
    public async Task CreateTag_ShouldCreateNormalizedTagForAdmin()
    {
        var tags = Substitute.For<ITagRepository>();
        tags.NameExistsAsync(" ASP.NET ", null, Arg.Any<CancellationToken>()).Returns(false);
        var service = new CreateTagService(CreateUnitOfWork(tags: tags), CreateCurrentUser(UserRole.Admin));

        var result = await service.Handle(new CreateTagRequestDto(" ASP.NET "), CancellationToken.None);

        result.Name.Should().Be("asp.net");
        await tags.Received(1).AddAsync(Arg.Is<Tag>(tag => tag.Name == "asp.net"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTag_ShouldRejectDuplicateName()
    {
        var tags = Substitute.For<ITagRepository>();
        tags.NameExistsAsync("backend", null, Arg.Any<CancellationToken>()).Returns(true);
        var service = new CreateTagService(CreateUnitOfWork(tags: tags), CreateCurrentUser(UserRole.Admin));

        var act = () => service.Handle(new CreateTagRequestDto("backend"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A tag with this name already exists");
    }

    [Fact]
    public async Task DeleteTag_ShouldRejectTagThatIsInUse()
    {
        var tagId = Guid.NewGuid();
        var tag = Tag.Create("backend");
        var tags = Substitute.For<ITagRepository>();
        tags.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(tag);
        tags.IsInUseAsync(tagId, Arg.Any<CancellationToken>()).Returns(true);
        var service = new DeleteTagService(CreateUnitOfWork(tags: tags), CreateCurrentUser(UserRole.Admin));

        var act = () => service.Handle(tagId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete a tag that is assigned to videos");
    }

    [Fact]
    public async Task UpdateTag_ShouldRenameTagForAdmin()
    {
        var tagId = Guid.NewGuid();
        var tag = Tag.Create("backend");
        var tags = Substitute.For<ITagRepository>();
        tags.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns(tag);
        tags.NameExistsAsync("api", tagId, Arg.Any<CancellationToken>()).Returns(false);
        var service = new UpdateTagService(CreateUnitOfWork(tags: tags), CreateCurrentUser(UserRole.Admin));

        var result = await service.Handle(tagId, new UpdateTagRequestDto(" API "), CancellationToken.None);

        result.Name.Should().Be("api");
        tag.Name.Should().Be("api");
    }

    private static IUnitOfWork CreateUnitOfWork(ITagRepository? tags = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Tags.Returns(tags ?? Substitute.For<ITagRepository>());
        return unitOfWork;
    }

    private static ICurrentUserService CreateCurrentUser(UserRole role)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(role);
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }
}
