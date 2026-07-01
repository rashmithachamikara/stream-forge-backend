using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

public sealed class VideoTranscriptChunkConfiguration : IEntityTypeConfiguration<VideoTranscriptChunk>
{
    private const string _trigramIndexOperator = "gin_trgm_ops";
    private const string _searchVectorPropertyName = "SearchVector";
    private const string _searchConfiguration = "english";

    public void Configure(EntityTypeBuilder<VideoTranscriptChunk> builder)
    {
        builder.ToTable("VideoTranscriptChunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Language)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(chunk => chunk.Content)
            .IsRequired();

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

        builder.HasIndex(_searchVectorPropertyName)
            .HasMethod("GIN")
            .HasDatabaseName("IX_VideoTranscriptChunks_SearchVector");

        builder.HasIndex(chunk => chunk.Content)
            .HasMethod("GIN")
            .HasOperators(_trigramIndexOperator)
            .HasDatabaseName("IX_VideoTranscriptChunks_Content_Trgm");

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
