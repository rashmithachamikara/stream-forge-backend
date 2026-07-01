using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "VideoTranscriptChunks",
                type: "vector",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmbeddingDimensions",
                table: "VideoTranscriptChunks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingGeneratedAt",
                table: "VideoTranscriptChunks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "VideoTranscriptChunks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingProvider",
                table: "VideoTranscriptChunks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_Embedding_Hnsw",
                table: "VideoTranscriptChunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_EmbeddingProvider_EmbeddingModel",
                table: "VideoTranscriptChunks",
                columns: new[] { "EmbeddingProvider", "EmbeddingModel" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_VideoId_Language_EmbeddingGeneratedAt",
                table: "VideoTranscriptChunks",
                columns: new[] { "VideoId", "Language", "EmbeddingGeneratedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptChunks_Embedding_Hnsw",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptChunks_EmbeddingProvider_EmbeddingModel",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptChunks_VideoId_Language_EmbeddingGeneratedAt",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingDimensions",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingGeneratedAt",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingProvider",
                table: "VideoTranscriptChunks");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
