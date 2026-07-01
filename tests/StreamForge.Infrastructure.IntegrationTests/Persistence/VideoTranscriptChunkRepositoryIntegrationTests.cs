using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Persistence.Repositories;
using StreamForge.Infrastructure.Tests.Support;

namespace StreamForge.Infrastructure.Tests.Persistence;

[Collection(PostgresIntegrationCollection.Name)]
[Trait("Category", "PostgresIntegration")]
public sealed class VideoTranscriptChunkRepositoryIntegrationTests
{
    private readonly PostgresIntegrationFixture _fixture;

    public VideoTranscriptChunkRepositoryIntegrationTests(PostgresIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task SearchFullTextAsync_ShouldPreferFullTextMatchesOverTrigramOnlyMatches()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var video = Video.Create("Transcript Video", null, owner.Id, status: VideoStatus.Ready);
        var exactTranscription = VideoTranscription.Create(video.Id, "en", "vtt", "videos/exact.vtt", "local-faster-whisper");
        var typoTranscription = VideoTranscription.Create(video.Id, "en", "srt", "videos/typo.srt", "local-faster-whisper");
        var exactChunk = VideoTranscriptChunk.Create(video.Id, exactTranscription.Id, "en", 20, 25, "broadcast summary for today");
        var typoChunk = VideoTranscriptChunk.Create(video.Id, typoTranscription.Id, "en", 10, 15, "broadcst summary for today");

        context.AddRange(owner, video, exactTranscription, typoTranscription, exactChunk, typoChunk);
        await context.SaveChangesAsync();

        var repository = new VideoTranscriptChunkRepository(context);

        var result = await repository.SearchFullTextAsync(video.Id, "broadcast", "en", 1, 10);

        result.Items.Select(chunk => chunk.Content).Should().ContainInOrder(
            "broadcast summary for today",
            "broadcst summary for today");
    }

    [IntegrationFact]
    public async Task SearchFullTextAsync_ShouldUseTrigramRecallForPartialAndRespectLanguagePaging()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var video = Video.Create("Transcript Video", null, owner.Id, status: VideoStatus.Ready);
        var englishTranscription = VideoTranscription.Create(video.Id, "en", "vtt", "videos/en.vtt", "local-faster-whisper");
        var englishChunkOne = VideoTranscriptChunk.Create(video.Id, englishTranscription.Id, "en", 5, 10, "broadcast bulletin begins");
        var englishChunkTwo = VideoTranscriptChunk.Create(video.Id, englishTranscription.Id, "en", 15, 20, "broadcast bulletin continues");

        var frenchTranscription = VideoTranscription.Create(video.Id, "fr", "vtt", "videos/fr.vtt", "local-faster-whisper");
        var frenchChunk = VideoTranscriptChunk.Create(video.Id, frenchTranscription.Id, "fr", 25, 30, "broadcast bulletin en francais");

        context.AddRange(owner, video, englishTranscription, frenchTranscription, englishChunkOne, englishChunkTwo, frenchChunk);
        await context.SaveChangesAsync();

        var repository = new VideoTranscriptChunkRepository(context);

        var result = await repository.SearchFullTextAsync(video.Id, "broad", "en", 1, 1);

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items[0].Language.Should().Be("en");
        result.Items[0].StartSeconds.Should().Be(5);
    }
}
