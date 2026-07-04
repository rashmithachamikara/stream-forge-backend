using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.TranscriptIntelligence;
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
            HybridSemanticWeight = 0.6d,
            HybridLexicalWeight = 0.4d,
            HybridMaxCandidates = 12,
            QaProvider = "disabled",
            QaMaxContextChunks = 8,
            QaMaxCitations = 5,
            QaTemperature = 0d,
            QaMaxOutputTokens = 512,
            QaProviderConfigs = new RagQaProviderConfigs
            {
                Gemini = new RagGeminiQaOptions
                {
                    Model = "gemini-3.1-flash-lite"
                }
            }
        };

        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting.Create("rag.enabled", "true"),
                SystemSetting.Create("rag.semanticSearch.enabled", "true"),
                SystemSetting.Create("rag.embedding.model", "mixedbread-ai/mxbai-embed-large-v1"),
                SystemSetting.Create("rag.embedding.batchSize", "32"),
                SystemSetting.Create("rag.retrieval.hybridSemanticWeight", "0.7"),
                SystemSetting.Create("rag.retrieval.hybridLexicalWeight", "0.3"),
                SystemSetting.Create("rag.retrieval.hybridMaxCandidates", "16"),
                SystemSetting.Create("rag.qa.maxCitations", "7"),
                SystemSetting.Create("rag.qa.temperature", "0.1"),
                SystemSetting.Create("rag.qa.maxOutputTokens", "256")
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
        result.HybridSemanticWeight.Should().Be(0.7d);
        result.HybridLexicalWeight.Should().Be(0.3d);
        result.HybridMaxCandidates.Should().Be(16);
        result.GeminiQaModel.Should().Be("gemini-3.1-flash-lite");
        result.QaMaxCitations.Should().Be(7);
        result.QaTemperature.Should().Be(0.1d);
        result.QaMaxOutputTokens.Should().Be(256);
        result.IsEmbeddingPipelineEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveRagSettings_ShouldPreferEncryptedAdminSecretOverConfigFallback()
    {
        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);

        var store = Substitute.For<ISystemSecretStoreService>();
        store.ResolveAsync(RagSecretKeys.GeminiApiKey, "config-key", Arg.Any<CancellationToken>())
            .Returns("db-key");

        var service = new ResolveRagSettingsService(
            unitOfWork,
            new RagOptions
            {
                QaProviderConfigs = new RagQaProviderConfigs
                {
                    Gemini = new RagGeminiQaOptions
                    {
                        ApiKey = "config-key"
                    }
                }
            },
            store);

        var result = await service.Handle(CancellationToken.None);

        result.GeminiQaApiKey.Should().Be("db-key");
    }

    [Fact]
    public async Task GetAdminRagSettings_ShouldReturnMaskedSecretStatuses()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());

        var store = Substitute.For<ISystemSecretStoreService>();
        store.GetStatusAsync(RagSecretKeys.GeminiApiKey, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new SecretConfigurationStatus(true, "****1234"));
        store.GetStatusAsync(RagSecretKeys.GrokApiKey, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new SecretConfigurationStatus(false, null));
        store.GetStatusAsync(RagSecretKeys.GroqApiKey, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new SecretConfigurationStatus(true, "****5678"));

        var defaults = new RagOptions
        {
            Enabled = true,
            SemanticSearchEnabled = true,
            VideoQuestionsEnabled = true,
            CrossVideoQuestionsEnabled = true,
            EmbeddingProvider = "local-sentence-transformer",
            EmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2",
            EmbeddingBatchSize = 100,
            RetrievalDefaultMode = "hybrid",
            SemanticTopK = 8,
            FullTextTopK = 8,
            HybridSemanticWeight = 0.6d,
            HybridLexicalWeight = 0.4d,
            HybridMaxCandidates = 12,
            QaProvider = "gemini",
            QaMaxContextChunks = 8,
            QaMaxCitations = 5,
            QaTemperature = 0d,
            QaMaxOutputTokens = 512
        };

        var service = new GetAdminRagSettingsService(
            new ResolveRagSettingsService(unitOfWork, defaults, store),
            defaults,
            store);

        var result = await service.Handle(CancellationToken.None);

        result.GeminiQaModel.Should().Be("gemini-3.1-flash-lite");
        result.QaModelCatalog.Should().Contain(entry => entry.Provider == "gemini" && entry.Models.Contains("gemini-2.5-flash"));
        result.GeminiApiKey.Should().Be(new SystemSecretStatusDto(true, "****1234"));
        result.GrokApiKey.Should().Be(new SystemSecretStatusDto(false, null));
        result.GroqApiKey.Should().Be(new SystemSecretStatusDto(true, "****5678"));
    }

    [Fact]
    public async Task UpdateAdminRagSettings_ShouldUpsertRuntimeSettingsAndManageSecrets()
    {
        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeyAsync("rag.enabled", Arg.Any<CancellationToken>())
            .Returns(SystemSetting.Create("rag.enabled", "false"));

        var addedSettings = new List<SystemSetting>();
        settingsRepo.AddAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var setting = call.Arg<SystemSetting>();
                addedSettings.Add(setting);
                return Task.FromResult(setting);
            });

        var updatedSettings = new List<SystemSetting>();
        settingsRepo.UpdateAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                updatedSettings.Add(call.Arg<SystemSetting>());
                return Task.CompletedTask;
            });

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);

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
            HybridSemanticWeight = 0.6d,
            HybridLexicalWeight = 0.4d,
            HybridMaxCandidates = 12,
            QaProvider = "disabled",
            QaMaxContextChunks = 8,
            QaMaxCitations = 5,
            QaTemperature = 0d,
            QaMaxOutputTokens = 512
        };

        var store = Substitute.For<ISystemSecretStoreService>();
        store.GetStatusAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new SecretConfigurationStatus(false, null));

        var getService = new GetAdminRagSettingsService(
            new ResolveRagSettingsService(unitOfWork, defaults, store),
            defaults,
            store);

        var service = new UpdateAdminRagSettingsService(unitOfWork, getService, store);
        var request = new UpdateAdminRagSettingsRequestDto(
            true,
            true,
            true,
            true,
            "local-sentence-transformer",
            "sentence-transformers/all-MiniLM-L6-v2",
            32,
            "hybrid",
            12,
            9,
            0.7d,
            0.3d,
            16,
            "groq",
            "gemini-2.5-pro",
            null,
            "llama-3.3-70b-versatile",
            6,
            4,
            0.1d,
            256,
            "gemini-secret",
            false,
            null,
            true,
            "groq-secret",
            false);

        var result = await service.Handle(request, CancellationToken.None);

        result.QaProvider.Should().Be("groq");
        result.GeminiQaModel.Should().Be("gemini-2.5-pro");
        result.GroqQaModel.Should().Be("llama-3.3-70b-versatile");
        updatedSettings.Should().ContainSingle(setting => setting.Key == "rag.enabled" && setting.Value == "True");
        addedSettings.Should().Contain(setting => setting.Key == "rag.qa.provider" && setting.Value == "groq");
        addedSettings.Should().Contain(setting => setting.Key == "rag.qa.providers.gemini.model" && setting.Value == "gemini-2.5-pro");
        addedSettings.Should().Contain(setting => setting.Key == "rag.qa.providers.groq.model" && setting.Value == "llama-3.3-70b-versatile");
        await store.Received(1).SetAsync(RagSecretKeys.GeminiApiKey, "gemini-secret", Arg.Any<CancellationToken>());
        await store.Received(1).ClearAsync(RagSecretKeys.GrokApiKey, Arg.Any<CancellationToken>());
        await store.Received(1).SetAsync(RagSecretKeys.GroqApiKey, "groq-secret", Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAdminRagSettings_ShouldRejectUnsupportedProviderModel()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());

        var store = Substitute.For<ISystemSecretStoreService>();
        var defaults = new RagOptions();
        var getService = new GetAdminRagSettingsService(
            new ResolveRagSettingsService(unitOfWork, defaults, store),
            defaults,
            store);

        var service = new UpdateAdminRagSettingsService(unitOfWork, getService, store);
        var request = new UpdateAdminRagSettingsRequestDto(
            false,
            false,
            false,
            false,
            "local-sentence-transformer",
            "sentence-transformers/all-MiniLM-L6-v2",
            100,
            "hybrid",
            8,
            8,
            0.6d,
            0.4d,
            12,
            "gemini",
            "made-up-model",
            null,
            null,
            8,
            5,
            0d,
            512,
            null,
            false,
            null,
            false,
            null,
            false);

        var act = () => service.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported model*gemini*");
    }

    [Fact]
    public async Task ResolveRagSettings_ShouldPreferValidStoredQaModelAndFallbackWhenStoredModelIsInvalid()
    {
        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting.Create("rag.qa.providers.gemini.model", "not-a-real-model"),
                SystemSetting.Create("rag.qa.providers.groq.model", "llama-3.3-70b-versatile")
            ]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);

        var service = new ResolveRagSettingsService(
            unitOfWork,
            new RagOptions
            {
                QaProviderConfigs = new RagQaProviderConfigs
                {
                    Gemini = new RagGeminiQaOptions { Model = "gemini-3.1-flash-lite" },
                    Groq = new RagGroqQaOptions { Model = "llama-3.3-70b-versatile" }
                }
            });

        var result = await service.Handle(CancellationToken.None);

        result.GeminiQaModel.Should().Be("gemini-3.1-flash-lite");
        result.GroqQaModel.Should().Be("llama-3.3-70b-versatile");
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
