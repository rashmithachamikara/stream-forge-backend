using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Pgvector;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

public sealed class VideoTranscriptChunkConfiguration : IEntityTypeConfiguration<VideoTranscriptChunk>
{
    private const string _trigramIndexOperator = "gin_trgm_ops";
    private const string _searchVectorPropertyName = "SearchVector";
    private const string _searchConfiguration = "english";
    private const string _embeddingColumnType = "vector";

    public void Configure(EntityTypeBuilder<VideoTranscriptChunk> builder)
    {
        builder.ToTable("VideoTranscriptChunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Language)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(chunk => chunk.Content)
            .IsRequired();

        builder.Property(chunk => chunk.Embedding)
            .HasConversion(
                embedding => embedding == null ? null : new Vector(embedding),
                embedding => embedding == null ? null : embedding.ToArray(),
                new ValueComparer<float[]?>(
                    (left, right) =>
                        ReferenceEquals(left, right) ||
                        left != null && right != null && left.SequenceEqual(right),
                    value =>
                        value == null
                            ? 0
                            : value.Aggregate(0, (current, item) => HashCode.Combine(current, item)),
                    value => value == null ? null : value.ToArray()))
            .HasColumnType(_embeddingColumnType);

        builder.Property(chunk => chunk.EmbeddingProvider)
            .HasMaxLength(100);

        builder.Property(chunk => chunk.EmbeddingModel)
            .HasMaxLength(200);

        builder.Property(chunk => chunk.EmbeddingDimensions);

        builder.Property(chunk => chunk.EmbeddingGeneratedAt);

        builder.Property(chunk => chunk.StartSeconds)
            .IsRequired();

        builder.Property(chunk => chunk.EndSeconds)
            .IsRequired();

        builder.Property(chunk => chunk.UpdatedAt)
            .IsRequired();

        builder.Property<NpgsqlTsVector>(_searchVectorPropertyName)
            .IsGeneratedTsVectorColumn(_searchConfiguration, nameof(VideoTranscriptChunk.Content));

        builder.HasIndex(chunk => chunk.VideoId)
            .HasDatabaseName("IX_VideoTranscriptChunks_VideoId");

        builder.HasIndex(chunk => chunk.TranscriptionId)
            .HasDatabaseName("IX_VideoTranscriptChunks_TranscriptionId");

        builder.HasIndex(chunk => new { chunk.VideoId, chunk.StartSeconds })
            .HasDatabaseName("IX_VideoTranscriptChunks_VideoId_StartSeconds");

        builder.HasIndex(chunk => new { chunk.VideoId, chunk.Language })
            .HasDatabaseName("IX_VideoTranscriptChunks_VideoId_Language");

        builder.HasIndex(chunk => new { chunk.VideoId, chunk.Language, chunk.EmbeddingGeneratedAt })
            .HasDatabaseName("IX_VideoTranscriptChunks_VideoId_Language_EmbeddingGeneratedAt");

        builder.HasIndex(chunk => new { chunk.EmbeddingProvider, chunk.EmbeddingModel })
            .HasDatabaseName("IX_VideoTranscriptChunks_EmbeddingProvider_EmbeddingModel");

        builder.HasIndex(_searchVectorPropertyName)
            .HasMethod("GIN")
            .HasDatabaseName("IX_VideoTranscriptChunks_SearchVector");

        builder.HasIndex(chunk => chunk.Content)
            .HasMethod("GIN")
            .HasOperators(_trigramIndexOperator)
            .HasDatabaseName("IX_VideoTranscriptChunks_Content_Trgm");

        builder.HasIndex(chunk => chunk.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("IX_VideoTranscriptChunks_Embedding_Hnsw");

        builder.HasOne(chunk => chunk.Video)
            .WithMany(video => video.VideoTranscriptChunks)
            .HasForeignKey(chunk => chunk.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(chunk => chunk.Transcription)
            .WithMany(transcription => transcription.TranscriptChunks)
            .HasForeignKey(chunk => chunk.TranscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
