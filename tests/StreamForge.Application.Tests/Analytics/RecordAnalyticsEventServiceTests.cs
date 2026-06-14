using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Analytics;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Analytics;

public sealed class RecordAnalyticsEventServiceTests
{
    [Fact]
    public async Task Handle_ShouldRejectWhenAnalyticsAreDisabled()
    {
        var sut = CreateService(new AnalyticsOptions { Enabled = false });
        var request = CreateRequest(AnalyticsEventType.Play);

        var act = () => sut.Handle(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("Analytics are disabled");
    }

    [Fact]
    public async Task Handle_ShouldRejectDisabledPauseEvents()
    {
        var sut = CreateService(new AnalyticsOptions { CollectPauseEvents = false });
        var request = CreateRequest(AnalyticsEventType.Pause);

        var act = () => sut.Handle(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("Pause event analytics are disabled");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreAnonymousEventsWhenAnonymousCollectionIsDisabled()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(false);

        var sut = CreateService(
            new AnalyticsOptions { CollectAnonymousEvents = false },
            currentUserService: currentUserService);
        var request = CreateRequest(AnalyticsEventType.Play);

        var result = await sut.Handle(Guid.NewGuid(), request, CancellationToken.None);

        result.EventRecorded.Should().BeFalse();
        result.ViewCountIncremented.Should().BeFalse();
    }

    private static RecordAnalyticsEventService CreateService(
        AnalyticsOptions options,
        ICurrentUserService? currentUserService = null)
    {
        return new RecordAnalyticsEventService(
            Substitute.For<IUnitOfWork>(),
            currentUserService ?? Substitute.For<ICurrentUserService>(),
            Substitute.For<IAuthorizationService>(),
            Substitute.For<IAnalyticsQueryService>(),
            Substitute.For<IRequestMetadataAccessor>(),
            options);
    }

    private static RecordAnalyticsEventRequestDto CreateRequest(AnalyticsEventType eventType)
    {
        return new RecordAnalyticsEventRequestDto(
            Guid.NewGuid(),
            eventType,
            DateTime.UtcNow,
            Position: 0,
            DurationWatched: 1);
    }
}
