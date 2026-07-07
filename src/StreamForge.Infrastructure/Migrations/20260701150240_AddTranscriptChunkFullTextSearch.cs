using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptChunkFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "VideoTranscriptChunks",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Content" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_SearchVector",
                table: "VideoTranscriptChunks",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptChunks_SearchVector",
                table: "VideoTranscriptChunks");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "VideoTranscriptChunks");
        }
    }
}
