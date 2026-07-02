using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.TranscriptIntelligence;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.TranscriptIntelligence;

public sealed class EmbeddingPipelineUseCaseTests
{
    [Fact]
    public async Task ResolveRagSettings_ShouldPreferSystemSettingsOverDefaults()
    {
        var defaults = new RagOptions
        {
            Enabled = false,
            SemanticSearchEnabled = false,
            VideoQuestionsEnabled = false,
            CrossVideoQuestionsEnabled = false,
            EmbeddingProvider = "local-sentence-transformer",
            EmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2",
            EmbeddingBatchSize = 100,
            RetrievalDefaultMode = "hybrid",
            SemanticTopK = 8,
            FullTextTopK = 8,
            QaProvider = "disabled",
            QaModel = string.Empty,
            QaMaxContextChunks = 8,
            QaMaxCitations = 5
        };

        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting.Create("rag.enabled", "true"),
                SystemSetting.Create("rag.semanticSearch.enabled", "true"),
                SystemSetting.Create("rag.embedding.model", "mixedbread-ai/mxbai-embed-large-v1"),
                SystemSetting.Create("rag.embedding.batchSize", "32"),
                SystemSetting.Create("rag.qa.maxCitations", "7")
            ]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);

        var service = new ResolveRagSettingsService(unitOfWork, defaults);

        var result = await service.Handle(CancellationToken.None);

        result.Enabled.Should().BeTrue();
        result.SemanticSearchEnabled.Should().BeTrue();
        result.EmbeddingProvider.Should().Be("local-sentence-transformer");
        result.EmbeddingModel.Should().Be("mixedbread-ai/mxbai-embed-large-v1");
        result.EmbeddingBatchSize.Should().Be(32);
        result.QaMaxCitations.Should().Be(7);
        result.IsEmbeddingPipelineEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateTranscriptEmbeddings_ShouldSkipWhenPipelineIsDisabled()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());

        var service = new GenerateTranscriptEmbeddingsService(
            unitOfWork,
            Substitute.For<ITranscriptEmbeddingProvider>(),
            new ResolveRagSettingsService(unitOfWork, new RagOptions()),
            Substitute.For<ILogger<GenerateTranscriptEmbeddingsService>>());

        var result = await service.Handle(Guid.NewGuid(), "en", CancellationToken.None);

        result.Executed.Should().BeFalse();
        result.Reason.Should().Be("Embedding pipeline is disabled.");
        await unitOfWork.VideoTranscriptChunks.DidNotReceiveWithAnyArgs().GetByVideoAndLanguageAsync(default, default!, default);
    }

    [Fact]
    public async Task GenerateTranscriptEmbeddings_ShouldBatchChunksAndCallProvider()
    {
        var videoId = Guid.NewGuid();
        var transcriptionId = Guid.NewGuid();
        var chunks = Enumerable.Range(0, 3)
            .Select(index => VideoTranscriptChunk.Create(
                videoId,
                transcriptionId,
                "en",
                index * 5,
                index * 5 + 4,
                $"chunk {index + 1}"))
            .ToArray();

        var chunksRepo = Substitute.For<IVideoTranscriptChunkRepository>();
        chunksRepo.GetByVideoAndLanguageAsync(videoId, "en", Arg.Any<CancellationToken>())
            .Returns(chunks);
        chunksRepo.UpdateAsync(Arg.Any<VideoTranscriptChunk>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting.Create("rag.enabled", "true"),
                SystemSetting.Create("rag.semanticSearch.enabled", "true"),
                SystemSetting.Create("rag.embedding.batchSize", "2")
            ]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);
        unitOfWork.VideoTranscriptChunks.Returns(chunksRepo);

        var provider = Substitute.For<ITranscriptEmbeddingProvider>();
        provider.GenerateEmbeddingsAsync(Arg.Any<TranscriptEmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<TranscriptEmbeddingRequest>();
                return new TranscriptEmbeddingBatchResult(
                    request.Provider,
                    request.Model,
                    384,
                    request.Items
                        .Select(item => new TranscriptEmbeddingResultItem(item.ChunkId, Enumerable.Repeat(0.1f, 384).ToArray()))
                        .ToArray());
            });

        var defaults = new RagOptions
        {
            Enabled = false,
            SemanticSearchEnabled = false,
            EmbeddingProvider = "local-sentence-transformer",
            EmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2",
            EmbeddingBatchSize = 100
        };

        var service = new GenerateTranscriptEmbeddingsService(
            unitOfWork,
            provider,
            new ResolveRagSettingsService(unitOfWork, defaults),
            Substitute.For<ILogger<GenerateTranscriptEmbeddingsService>>());

        var result = await service.Handle(videoId, "en", CancellationToken.None);

        result.Executed.Should().BeTrue();
        result.ChunksProcessed.Should().Be(3);
        result.BatchCount.Should().Be(2);
        result.VectorSize.Should().Be(384);
        chunks.Should().OnlyContain(chunk =>
            chunk.Embedding != null &&
            chunk.EmbeddingProvider == "local-sentence-transformer" &&
            chunk.EmbeddingModel == "sentence-transformers/all-MiniLM-L6-v2" &&
            chunk.EmbeddingGeneratedAt != null);

        await provider.Received(2).GenerateEmbeddingsAsync(Arg.Any<TranscriptEmbeddingRequest>(), Arg.Any<CancellationToken>());
        await chunksRepo.Received(1).GetByVideoAndLanguageAsync(videoId, "en", Arg.Any<CancellationToken>());
        await chunksRepo.Received(3).UpdateAsync(Arg.Any<VideoTranscriptChunk>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
