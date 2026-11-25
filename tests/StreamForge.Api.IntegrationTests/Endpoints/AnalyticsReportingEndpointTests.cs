using System.Net;
using FluentAssertions;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.DTOs.Content;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class AnalyticsReportingEndpointTests(ApiIntegrationFixture fixture)
{
    private const string _ownerAnalyticsUrl = "/api/v1/me/analytics";
    private const string _adminAnalyticsUrl = "/api/v1/admin/analytics";
    private const string _ownerTitle = "Opening, \"quoted\"";
    private const string _secondOwnerTitle = "Owner follow-up";
    private const string _otherTitle = "Another creator's video";
    private const string _csvHeader = "VideoId,Title,Views,TotalWatchTime,Likes,Dislikes,Comments,EngagementScore";
    private static readonly DateTime _firstDay = new(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string _reportRange = $"?from={_firstDay:O}&to={_firstDay.AddDays(2).AddSeconds(-1):O}";

    [ApiIntegrationFact]
    public async Task OwnerReports_ShouldReturnVideoMetricsAndScopeTotalsToOwnedVideos()
    {
        await fixture.ResetDatabaseAsync();
        var owner = await fixture.CreateUserAsync(UserRole.Editor, "analytics-owner@example.com");
        var otherOwner = await fixture.CreateUserAsync(UserRole.Editor, "analytics-other@example.com");
        var videos = await SeedAnalyticsAsync(owner.User.Id, otherOwner.User.Id);
        var client = fixture.Client;
        var videoSummaryUrl = $"/api/v1/videos/{videos.First}/analytics/summary{_reportRange}";

        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, videoSummaryUrl);
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, videoSummaryUrl, otherOwner.AccessToken);
        var videoSummary = await client.ReadOkJsonAsync<AnalyticsSummaryDto>(HttpMethod.Get, videoSummaryUrl, owner.AccessToken);
        videoSummary.Should().Be(new AnalyticsSummaryDto(1, 1, 15, 15m, 100m, 1));

        var ownerSummaryUrl = $"{_ownerAnalyticsUrl}/summary{_reportRange}";
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, ownerSummaryUrl);
        var ownerSummary = await client.ReadOkJsonAsync<AnalyticsSummaryDto>(HttpMethod.Get, ownerSummaryUrl, owner.AccessToken);
        ownerSummary.Should().Be(new AnalyticsSummaryDto(2, 2, 25, 12.5m, 50m, 1));

        var topVideos = await client.ReadOkJsonAsync<PagedResponseDto<RankedVideoAnalyticsDto>>(
            HttpMethod.Get, $"{_ownerAnalyticsUrl}/top-videos{_reportRange}", owner.AccessToken);
        topVideos.TotalCount.Should().Be(2);
        topVideos.Items.Select(video => video.VideoId).Should().Equal(videos.First, videos.Second);
    }

    [ApiIntegrationFact]
    public async Task AdminReports_ShouldRequireAdminAndReturnPlatformTotalsAndRankings()
    {
        await fixture.ResetDatabaseAsync();
        var admin = await fixture.CreateUserAsync(UserRole.Admin, "analytics-admin@example.com");
        var owner = await fixture.CreateUserAsync(UserRole.Editor, "analytics-owner@example.com");
        var otherOwner = await fixture.CreateUserAsync(UserRole.Editor, "analytics-other@example.com");
        var videos = await SeedAnalyticsAsync(owner.User.Id, otherOwner.User.Id);
        var client = fixture.Client;
        var summaryUrl = $"{_adminAnalyticsUrl}/summary{_reportRange}";
        var seriesUrl = $"{_adminAnalyticsUrl}/views-over-time{_reportRange}";
        var rankingUrl = $"{_adminAnalyticsUrl}/most-watched-videos{_reportRange}";

        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, summaryUrl);
        await client.AssertStatusAsync(HttpStatusCode.Forbidden, HttpMethod.Get, summaryUrl, owner.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.Forbidden, HttpMethod.Get, seriesUrl, owner.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.Forbidden, HttpMethod.Get, rankingUrl, owner.AccessToken);

        var summary = await client.ReadOkJsonAsync<AnalyticsSummaryDto>(HttpMethod.Get, summaryUrl, admin.AccessToken);
        summary.Should().Be(new AnalyticsSummaryDto(4, 4, 57, 14.25m, 25m, 1));
        var timeSeries = await client.ReadOkJsonAsync<AnalyticsTimeSeriesPointDto[]>(HttpMethod.Get, seriesUrl, admin.AccessToken);
        timeSeries.Should().Equal(new AnalyticsTimeSeriesPointDto(_firstDay, 1), new AnalyticsTimeSeriesPointDto(_firstDay.AddDays(1), 3));

        var ranking = await client.ReadOkJsonAsync<PagedResponseDto<RankedVideoAnalyticsDto>>(HttpMethod.Get, rankingUrl, admin.AccessToken);
        ranking.TotalCount.Should().Be(3);
        ranking.Items.Select(video => video.VideoId).Should().Equal(videos.Other, videos.First, videos.Second);
        ranking.Items.Select(video => video.ViewCount).Should().Equal(2, 1, 1);
    }

    [ApiIntegrationFact]
    public async Task CsvExports_ShouldDownloadScopedRowsAndRequireAdminForPlatformReport()
    {
        await fixture.ResetDatabaseAsync();
        var admin = await fixture.CreateUserAsync(UserRole.Admin, "analytics-admin@example.com");
        var owner = await fixture.CreateUserAsync(UserRole.Editor, "analytics-owner@example.com");
        var otherOwner = await fixture.CreateUserAsync(UserRole.Editor, "analytics-other@example.com");
        var videos = await SeedAnalyticsAsync(owner.User.Id, otherOwner.User.Id);
        var client = fixture.Client;
        var ownerReportUrl = $"{_ownerAnalyticsUrl}/reports/videos{_reportRange}";
        var platformReportUrl = $"{_adminAnalyticsUrl}/reports/overview{_reportRange}";

        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, ownerReportUrl);
        await client.AssertStatusAsync(HttpStatusCode.Forbidden, HttpMethod.Get, platformReportUrl, owner.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.BadRequest, HttpMethod.Get, $"{ownerReportUrl}&format=json", owner.AccessToken);

        var firstRow = $"{videos.First},\"Opening, \"\"quoted\"\"\",1,15,0,0,0,0";
        var secondRow = $"{videos.Second},\"{_secondOwnerTitle}\",1,10,0,0,0,0";
        var otherRow = $"{videos.Other},\"{_otherTitle}\",2,32,0,0,0,0";
        var ownerRows = await ReadCsvAsync(ownerReportUrl, owner.AccessToken, "my-video-analytics-");
        ownerRows.Should().Equal(_csvHeader, firstRow, secondRow);

        var platformRows = await ReadCsvAsync(platformReportUrl, admin.AccessToken, "platform-analytics-");
        platformRows.Should().Equal(_csvHeader, otherRow, firstRow, secondRow);
    }

    private async Task<string[]> ReadCsvAsync(string url, string accessToken, string fileNamePrefix)
    {
        using var response = await fixture.Client.SendRequestAsync(HttpMethod.Get, url, accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("text/csv");
        var disposition = response.Content.Headers.ContentDisposition;
        (disposition?.DispositionType).Should().Be("attachment");
        var fileName = (disposition?.FileNameStar ?? disposition?.FileName)?.Trim('"');
        fileName.Should().StartWith(fileNamePrefix).And.EndWith(".csv");

        var csv = await response.Content.ReadAsStringAsync();
        return csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    private async Task<(Guid First, Guid Second, Guid Other)> SeedAnalyticsAsync(Guid ownerId, Guid otherOwnerId)
    {
        await using var context = fixture.CreateContext();
        var first = Video.Create(_ownerTitle, null, ownerId);
        var second = Video.Create(_secondOwnerTitle, null, ownerId);
        var other = Video.Create(_otherTitle, null, otherOwnerId);
        var firstSession = Guid.NewGuid();
        var firstDay = _firstDay.AddHours(10);
        var secondDay = firstDay.AddDays(1);

        context.Videos.AddRange(first, second, other);
        context.AnalyticsEvents.AddRange(
            AnalyticsEvent.Create(first.Id, firstSession, AnalyticsEventType.Play, "127.0.0.1", eventTime: firstDay, durationWatched: 8),
            AnalyticsEvent.Create(first.Id, firstSession, AnalyticsEventType.Complete, "127.0.0.1", eventTime: firstDay.AddSeconds(8), durationWatched: 7),
            AnalyticsEvent.Create(first.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.1", eventTime: firstDay, durationWatched: 2),
            AnalyticsEvent.Create(first.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.1", eventTime: firstDay.AddDays(-1), durationWatched: 30),
            AnalyticsEvent.Create(second.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.1", eventTime: secondDay, durationWatched: 10),
            AnalyticsEvent.Create(other.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.1", eventTime: secondDay, durationWatched: 20),
            AnalyticsEvent.Create(other.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.1", eventTime: secondDay, durationWatched: 12));
        await context.SaveChangesAsync();
        return (first.Id, second.Id, other.Id);
    }
}
