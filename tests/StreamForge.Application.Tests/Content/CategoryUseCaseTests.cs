using FluentAssertions;
using NSubstitute;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Content;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Content;

public sealed class CategoryUseCaseTests
{
    [Fact]
    public async Task CreateCategory_ShouldRequireAdmin()
    {
        var service = new CreateCategoryService(CreateUnitOfWork(), CreateCurrentUser(UserRole.Editor));
        var request = new CreateCategoryRequestDto("Education", null, null, 1);

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only administrators can perform this action");
    }

    [Fact]
    public async Task CreateCategory_ShouldRejectDuplicateName()
    {
        var categories = Substitute.For<ICategoryRepository>();
        categories.NameExistsAsync("Education", null, Arg.Any<CancellationToken>()).Returns(true);
        var service = new CreateCategoryService(CreateUnitOfWork(categories: categories), CreateCurrentUser(UserRole.Admin));
        var request = new CreateCategoryRequestDto("Education", null, null, 1);

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A category with this name already exists");
    }

    [Fact]
    public async Task DeleteCategory_ShouldRejectCategoryWithVideos()
    {
        var categoryId = Guid.NewGuid();
        var categories = Substitute.For<ICategoryRepository>();
        categories.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create("Education"));
        categories.HasSubcategoriesAsync(categoryId, Arg.Any<CancellationToken>()).Returns(false);
        categories.HasVideosAsync(categoryId, Arg.Any<CancellationToken>()).Returns(true);

        var service = new DeleteCategoryService(CreateUnitOfWork(categories: categories), CreateCurrentUser(UserRole.Admin));

        var act = () => service.Handle(categoryId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete a category that is assigned to videos");
    }

    private static IUnitOfWork CreateUnitOfWork(ICategoryRepository? categories = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Categories.Returns(categories ?? Substitute.For<ICategoryRepository>());
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
