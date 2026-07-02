using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Pgvector;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class VideoTranscriptChunkRepository : BaseRepository<VideoTranscriptChunk>, IVideoTranscriptChunkRepository
{
    private const float _trigramWordSimilarityThreshold = 0.2f;
    private const string _searchVectorPropertyName = "SearchVector";
    private const string _searchConfiguration = "english";

    public VideoTranscriptChunkRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task DeleteByVideoAndLanguageAsync(Guid videoId, string language, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = language.Trim().ToLowerInvariant();
        var rows = await DbContext.VideoTranscriptChunks
            .Where(chunk => chunk.VideoId == videoId && chunk.Language == normalizedLanguage)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return;
        }

        DbContext.VideoTranscriptChunks.RemoveRange(rows);
    }

    public async Task<IReadOnlyList<VideoTranscriptChunk>> GetByTranscriptionIdAsync(
        Guid transcriptionId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.TranscriptionId == transcriptionId)
            .OrderBy(chunk => chunk.StartSeconds)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscriptChunk>> GetByVideoAndLanguageAsync(
        Guid videoId,
        string language,
        CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = language.Trim().ToLowerInvariant();
        return await DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.VideoId == videoId && chunk.Language == normalizedLanguage)
            .OrderBy(chunk => chunk.StartSeconds)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedQueryResult<TranscriptLexicalChunkMatch>> SearchLexicalByVideoAsync(
        Guid videoId,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.Trim();
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();

        var query = DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.VideoId == videoId);

        if (!string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            query = query.Where(chunk => chunk.Language == normalizedLanguage);
        }

        return await SearchLexicalAsync(
            query,
            normalizedSearchTerm,
            page,
            pageSize,
            candidateCount,
            includeVideoTitle: false,
            cancellationToken);
    }

    public async Task<PagedQueryResult<TranscriptLexicalChunkMatch>> SearchLexicalAcrossVideosAsync(
        IReadOnlyCollection<Guid> videoIds,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default)
    {
        if (videoIds.Count == 0)
        {
            return new PagedQueryResult<TranscriptLexicalChunkMatch>([], 0, page, pageSize);
        }

        var normalizedSearchTerm = searchTerm.Trim();
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();
        var scopedIds = videoIds.Distinct().ToArray();

        var query = DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => scopedIds.Contains(chunk.VideoId));

        if (!string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            query = query.Where(chunk => chunk.Language == normalizedLanguage);
        }

        return await SearchLexicalAsync(
            query,
            normalizedSearchTerm,
            page,
            pageSize,
            candidateCount,
            includeVideoTitle: true,
            cancellationToken);
    }

    public async Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticByVideoAsync(
        Guid videoId,
        float[] queryEmbedding,
        string embeddingProvider,
        string embeddingModel,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();
        return await SearchSemanticAsync(
            "chunk.\"VideoId\" = @videoId",
            [
                CreateQueryEmbeddingParameter(queryEmbedding),
                new NpgsqlParameter<Guid>("videoId", videoId),
                new NpgsqlParameter<string>("embeddingProvider", embeddingProvider.Trim()),
                new NpgsqlParameter<string>("embeddingModel", embeddingModel.Trim()),
                new NpgsqlParameter<string?>("language", normalizedLanguage),
                new NpgsqlParameter<int>("candidateCount", candidateCount),
                new NpgsqlParameter<int>("offset", (page - 1) * pageSize),
                new NpgsqlParameter<int>("pageSize", pageSize)
            ],
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticAcrossVideosAsync(
        IReadOnlyCollection<Guid> videoIds,
        float[] queryEmbedding,
        string embeddingProvider,
        string embeddingModel,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default)
    {
        if (videoIds.Count == 0)
        {
            return new PagedQueryResult<TranscriptSemanticChunkMatch>([], 0, page, pageSize);
        }

        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();
        return await SearchSemanticAsync(
            "chunk.\"VideoId\" = ANY(@videoIds)",
            [
                CreateQueryEmbeddingParameter(queryEmbedding),
                new NpgsqlParameter<Guid[]>("videoIds", videoIds.Distinct().ToArray()),
                new NpgsqlParameter<string>("embeddingProvider", embeddingProvider.Trim()),
                new NpgsqlParameter<string>("embeddingModel", embeddingModel.Trim()),
                new NpgsqlParameter<string?>("language", normalizedLanguage),
                new NpgsqlParameter<int>("candidateCount", candidateCount),
                new NpgsqlParameter<int>("offset", (page - 1) * pageSize),
                new NpgsqlParameter<int>("pageSize", pageSize)
            ],
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<PagedQueryResult<VideoTranscriptChunk>> SearchFullTextAsync(
        Guid videoId,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.Trim();
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();
        var query = DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.VideoId == videoId);

        if (!string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            query = query.Where(chunk => chunk.Language == normalizedLanguage);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var rankedQuery = query
                .Select(chunk => new
                {
                    Chunk = chunk,
                    IsFullTextMatch = EF.Property<NpgsqlTsVector>(chunk, _searchVectorPropertyName)
                        .Matches(EF.Functions.WebSearchToTsQuery(_searchConfiguration, normalizedSearchTerm)),
                    FullTextRank = EF.Property<NpgsqlTsVector>(chunk, _searchVectorPropertyName)
                        .RankCoverDensity(EF.Functions.WebSearchToTsQuery(_searchConfiguration, normalizedSearchTerm)),
                    TrigramWordSimilarity = EF.Functions.TrigramsWordSimilarity(normalizedSearchTerm, chunk.Content)
                })
                .Where(item => item.IsFullTextMatch || item.TrigramWordSimilarity >= _trigramWordSimilarityThreshold);

            var totalCount = await rankedQuery.CountAsync(cancellationToken);
            var items = await rankedQuery
                .OrderByDescending(item => item.IsFullTextMatch)
                .ThenByDescending(item => item.FullTextRank)
                .ThenByDescending(item => item.TrigramWordSimilarity)
                .ThenBy(item => item.Chunk.StartSeconds)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => item.Chunk)
                .ToListAsync(cancellationToken);

            return new PagedQueryResult<VideoTranscriptChunk>(items, totalCount, page, pageSize);
        }

        var fallbackTotalCount = await query.CountAsync(cancellationToken);
        var fallbackItems = await query
            .OrderBy(chunk => chunk.StartSeconds)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<VideoTranscriptChunk>(fallbackItems, fallbackTotalCount, page, pageSize);
    }

    private async Task<PagedQueryResult<TranscriptLexicalChunkMatch>> SearchLexicalAsync(
        IQueryable<VideoTranscriptChunk> baseQuery,
        string normalizedSearchTerm,
        int page,
        int pageSize,
        int candidateCount,
        bool includeVideoTitle,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            return new PagedQueryResult<TranscriptLexicalChunkMatch>([], 0, page, pageSize);
        }

        var rankedQuery = baseQuery
            .Select(chunk => new TranscriptLexicalChunkProjection
            {
                ChunkId = chunk.Id,
                VideoId = chunk.VideoId,
                TranscriptionId = chunk.TranscriptionId,
                Language = chunk.Language,
                StartSeconds = chunk.StartSeconds,
                EndSeconds = chunk.EndSeconds,
                Content = chunk.Content,
                IsFullTextMatch = EF.Property<NpgsqlTsVector>(chunk, _searchVectorPropertyName)
                    .Matches(EF.Functions.WebSearchToTsQuery(_searchConfiguration, normalizedSearchTerm)),
                FullTextRank = EF.Property<NpgsqlTsVector>(chunk, _searchVectorPropertyName)
                    .RankCoverDensity(EF.Functions.WebSearchToTsQuery(_searchConfiguration, normalizedSearchTerm)),
                TrigramWordSimilarity = EF.Functions.TrigramsWordSimilarity(normalizedSearchTerm, chunk.Content),
                VideoTitle = includeVideoTitle ? chunk.Video.Title : null
            })
            .Where(item => item.IsFullTextMatch || item.TrigramWordSimilarity >= _trigramWordSimilarityThreshold);

        var totalCount = await rankedQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedQueryResult<TranscriptLexicalChunkMatch>([], 0, page, pageSize);
        }

        var items = await rankedQuery
            .OrderByDescending(item => item.IsFullTextMatch)
            .ThenByDescending(item => item.FullTextRank)
            .ThenByDescending(item => item.TrigramWordSimilarity)
            .ThenBy(item => item.StartSeconds)
            .ThenBy(item => item.ChunkId)
            .Take(candidateCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<TranscriptLexicalChunkMatch>(
            items.Select(item => new TranscriptLexicalChunkMatch(
                    item.ChunkId,
                    item.VideoId,
                    item.TranscriptionId,
                    item.Language,
                    item.StartSeconds,
                    item.EndSeconds,
                    item.Content,
                    NormalizeLexicalScore(item.IsFullTextMatch, item.FullTextRank, item.TrigramWordSimilarity),
                    item.VideoTitle))
                .ToArray(),
            totalCount,
            page,
            pageSize);
    }

    private async Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticAsync(
        string scopePredicate,
        IReadOnlyList<NpgsqlParameter> parameters,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var countSql = $"""
            WITH ranked AS (
                SELECT
                    chunk."Id" AS "ChunkId",
                    chunk."VideoId" AS "VideoId",
                    chunk."TranscriptionId" AS "TranscriptionId",
                    chunk."Language" AS "Language",
                    chunk."StartSeconds" AS "StartSeconds",
                    chunk."EndSeconds" AS "EndSeconds",
                    chunk."Content" AS "Content",
                    video."Title" AS "VideoTitle",
                    chunk."Embedding" <=> @queryEmbedding AS "Distance"
                FROM "VideoTranscriptChunks" AS chunk
                INNER JOIN "Videos" AS video ON video."Id" = chunk."VideoId"
                WHERE {scopePredicate}
                  AND chunk."Embedding" IS NOT NULL
                  AND chunk."EmbeddingProvider" = @embeddingProvider
                  AND chunk."EmbeddingModel" = @embeddingModel
                  AND (@language IS NULL OR chunk."Language" = @language)
                ORDER BY "Distance" ASC, chunk."StartSeconds" ASC, chunk."Id" ASC
                LIMIT @candidateCount
            )
            SELECT COUNT(*) AS "Value"
            FROM ranked
            """;

        var totalCount = await DbContext.Database
            .SqlQueryRaw<int>(countSql, parameters.ToArray())
            .SingleAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PagedQueryResult<TranscriptSemanticChunkMatch>([], 0, page, pageSize);
        }

        var itemsSql = $"""
            WITH ranked AS (
                SELECT
                    chunk."Id" AS "ChunkId",
                    chunk."VideoId" AS "VideoId",
                    chunk."TranscriptionId" AS "TranscriptionId",
                    chunk."Language" AS "Language",
                    chunk."StartSeconds" AS "StartSeconds",
                    chunk."EndSeconds" AS "EndSeconds",
                    chunk."Content" AS "Content",
                    video."Title" AS "VideoTitle",
                    chunk."Embedding" <=> @queryEmbedding AS "Distance"
                FROM "VideoTranscriptChunks" AS chunk
                INNER JOIN "Videos" AS video ON video."Id" = chunk."VideoId"
                WHERE {scopePredicate}
                  AND chunk."Embedding" IS NOT NULL
                  AND chunk."EmbeddingProvider" = @embeddingProvider
                  AND chunk."EmbeddingModel" = @embeddingModel
                  AND (@language IS NULL OR chunk."Language" = @language)
                ORDER BY "Distance" ASC, chunk."StartSeconds" ASC, chunk."Id" ASC
                LIMIT @candidateCount
            )
            SELECT
                "ChunkId",
                "VideoId",
                "TranscriptionId",
                "Language",
                "StartSeconds",
                "EndSeconds",
                "Content",
                GREATEST(0::double precision, LEAST(1::double precision, 1 - "Distance")) AS "Score",
                "VideoTitle"
            FROM ranked
            ORDER BY "Score" DESC, "StartSeconds" ASC, "ChunkId" ASC
            OFFSET @offset
            LIMIT @pageSize
            """;

        var items = await DbContext.Database
            .SqlQueryRaw<TranscriptSemanticChunkRow>(itemsSql, parameters.ToArray())
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<TranscriptSemanticChunkMatch>(
            items.Select(item => new TranscriptSemanticChunkMatch(
                    item.ChunkId,
                    item.VideoId,
                    item.TranscriptionId,
                    item.Language,
                    item.StartSeconds,
                    item.EndSeconds,
                    item.Content,
                    item.Score,
                    item.VideoTitle))
                .ToArray(),
            totalCount,
            page,
            pageSize);
    }

    private static NpgsqlParameter CreateQueryEmbeddingParameter(float[] queryEmbedding) =>
        new("queryEmbedding", new Vector(queryEmbedding));

    private static double NormalizeLexicalScore(bool isFullTextMatch, float fullTextRank, double trigramWordSimilarity)
    {
        var normalizedRank = Math.Clamp(fullTextRank, 0f, 1f);
        var normalizedTrigram = Math.Clamp(trigramWordSimilarity, 0f, 1f);

        var score = isFullTextMatch
            ? 0.7d + (normalizedRank * 0.2d) + (normalizedTrigram * 0.1d)
            : normalizedTrigram * 0.5d;

        return Math.Clamp(score, 0d, 1d);
    }

    private sealed class TranscriptLexicalChunkProjection
    {
        public Guid ChunkId { get; init; }

        public Guid VideoId { get; init; }

        public Guid TranscriptionId { get; init; }

        public string Language { get; init; } = string.Empty;

        public double StartSeconds { get; init; }

        public double EndSeconds { get; init; }

        public string Content { get; init; } = string.Empty;

        public bool IsFullTextMatch { get; init; }

        public float FullTextRank { get; init; }

        public double TrigramWordSimilarity { get; init; }

        public string? VideoTitle { get; init; }
    }

    private sealed class TranscriptSemanticChunkRow
    {
        public Guid ChunkId { get; init; }

        public Guid VideoId { get; init; }

        public Guid TranscriptionId { get; init; }

        public string Language { get; init; } = string.Empty;

        public double StartSeconds { get; init; }

        public double EndSeconds { get; init; }

        public string Content { get; init; } = string.Empty;

        public double Score { get; init; }

        public string? VideoTitle { get; init; }
    }
}
